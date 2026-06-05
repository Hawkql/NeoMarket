using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using B2B.Api.Tests.Infrastructure;
using B2B.Domain.Products;
using B2B.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;


namespace B2B.Api.Tests.Products
{
    [Collection("Sequential")]
    public sealed class ViewProductTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ViewProductTests(CustomWebApplicationFactory factory) => _factory = factory;

        private HttpClient AuthClient(Guid sellerId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return client;
        }

        private async Task<Guid> CreateProductAsync(HttpClient client)
        {
            var body = new
            {
                category_id = TestData.CategoryId,
                title = "Card Product",
                description = "desc",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = new[] { new { name = "Бренд", value = "Apple" } }
            };
            var resp = await client.PostAsJsonAsync("/api/v1/products", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
        }

        private async Task CreateSkuAsync(HttpClient client, Guid productId)
        {
            var body = new
            {
                product_id = productId,
                name = "SKU-1",
                price = 1000000,
                discount = 0,
                cost_price = 500000,
                article = "ART-1",
                images = new[] { new { url = "/s3/sku.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            (await client.PostAsJsonAsync("/api/v1/skus", body))
                .StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // ── продавец видит свою карточку целиком ──
        [Fact(DisplayName = "get_moderated_product_returns_full_payload")]
        public async Task get_moderated_product_returns_full_payload()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            var resp = await client.GetAsync($"/api/v1/products/{productId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await resp.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json).RootElement;

            root.GetProperty("id").GetGuid().Should().Be(productId);
            root.GetProperty("seller_id").GetGuid().Should().Be(sellerId);
            root.GetProperty("title").GetString().Should().Be("Card Product");
            root.GetProperty("characteristics").GetArrayLength().Should().Be(1);
            root.GetProperty("images").GetArrayLength().Should().Be(1);
            root.TryGetProperty("field_reports", out _).Should().BeTrue("карточка содержит field_reports");
        }

        // ── BLOCKED-товар показывает blocking_reason_id + moderator_comment + field_reports ──
        // ── BLOCKED-товар показывает blocking_reason (объект) + field_reports ──
        [Fact(DisplayName = "get_blocked_product_returns_blocking_reason_and_field_reports")]
        public async Task get_blocked_product_returns_blocking_reason_and_field_reports()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);
            await CreateSkuAsync(client, productId);     // → OnModeration (нужно для Block)

            // Блокируем напрямую через домен. BlockingReason: (id, title, comment).
            var reasonId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var product = await db.Products
                    .Include(p => p.FieldReports)
                    .FirstAsync(p => p.Id == productId);
                product.Block(
                    new BlockingReason(reasonId, "Нарушение правил", "комментарий модератора"),
                    new List<(FieldReportTarget, Guid?, string)>
                    {
                (FieldReportTarget.Description, null, "Несоответствие описания")
                    },
                    new List<Guid>(),
                    DateTime.UtcNow);
                await db.SaveChangesAsync();
            }

            var resp = await client.GetAsync($"/api/v1/products/{productId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await resp.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json).RootElement;

            root.GetProperty("status").GetString().Should().Be("BLOCKED");

            // blocking_reason — вложенный объект {id, title, comment} по ProductDetailResponse
            var br = root.GetProperty("blocking_reason");
            br.ValueKind.Should().Be(JsonValueKind.Object);

            br.GetProperty("id").GetGuid().Should().Be(reasonId);
            br.GetProperty("title").GetString().Should().Be("Нарушение правил");
            br.GetProperty("comment").GetString().Should().Be("комментарий модератора");

            // Плоских legacy-полей быть не должно
            root.TryGetProperty("blocking_reason_id", out _).Should()
                .BeFalse("ProductDetailResponse не содержит плоских legacy-полей");
            root.TryGetProperty("moderator_comment", out _).Should()
                .BeFalse("ProductDetailResponse не содержит плоских legacy-полей");

            // field_reports — непустой массив
            var reports = root.GetProperty("field_reports");
            reports.GetArrayLength().Should().BeGreaterThan(0);
            reports[0].GetProperty("field_name").GetString().Should().Be("description");
        }

        // ── чужой товар → 404 (чтение скрывает существование) ──
        [Fact(DisplayName = "get_others_product_returns_404")]
        public async Task get_others_product_returns_404()
        {
            var ownerId = Guid.NewGuid();
            var productId = await CreateProductAsync(AuthClient(ownerId));

            var attacker = AuthClient(Guid.NewGuid());
            var resp = await attacker.GetAsync($"/api/v1/products/{productId}");

            resp.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "чужой товар при чтении не раскрывается (404, не 403)");
        }

        // ── несуществующий → 404 ──
        [Fact(DisplayName = "get_nonexistent_returns_404")]
        public async Task get_nonexistent_returns_404()
        {
            var client = AuthClient(Guid.NewGuid());
            var resp = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ── страховка: images SKU реально попадают в карточку (фикс из ревью US-05) ──
        [Fact(DisplayName = "get_product_includes_sku_images_in_card")]
        public async Task get_product_includes_sku_images_in_card()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            // SKU с двумя картинками
            var body = new
            {
                product_id = productId,
                name = "SKU-IMG",
                price = 1000000,
                discount = 0,
                cost_price = 500000,
                article = "ART-IMG",
                images = new[]
                {
                    new { url = "/s3/sku-1.jpg", ordering = 0 },
                    new { url = "/s3/sku-2.jpg", ordering = 1 }
                },
                characteristics = Array.Empty<object>()
            };
            (await client.PostAsJsonAsync("/api/v1/skus", body))
                .StatusCode.Should().Be(HttpStatusCode.Created);

            var resp = await client.GetAsync($"/api/v1/products/{productId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var root = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var sku = root.GetProperty("skus")[0];

            var imgs = sku.GetProperty("images");
            imgs.ValueKind.Should().Be(JsonValueKind.Array, "images SKU должны быть массивом");
            imgs.GetArrayLength().Should().Be(2, "обе картинки SKU должны попасть в карточку");

            sku.TryGetProperty("image_url", out _).Should()
                .BeFalse("скалярный image_url убран в пользу images[]");
        }
    }
}