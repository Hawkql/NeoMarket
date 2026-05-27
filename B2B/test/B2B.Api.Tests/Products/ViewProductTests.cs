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
        [Fact(DisplayName = "view_own_product_returns_full_card")]
        public async Task view_own_product_returns_full_card()
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

        // ── BLOCKED-товар показывает blocking_reason + field_reports ──
        [Fact(DisplayName = "view_blocked_shows_blocking_reason")]
        public async Task view_blocked_shows_blocking_reason()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);
            await CreateSkuAsync(client, productId);     // → OnModeration (нужно для Block)

            // Блокируем напрямую через домен с полевым отчётом
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var product = await db.Products
                    .Include(p => p.FieldReports)
                    .FirstAsync(p => p.Id == productId);
                product.Block(
                    new BlockingReason(Guid.NewGuid(), "Описание не соответствует", "комментарий модератора"),
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

            // blocking_reason — объект с title
            var br = root.GetProperty("blocking_reason");
            br.ValueKind.Should().Be(JsonValueKind.Object);
            br.GetProperty("title").GetString().Should().Be("Описание не соответствует");

            // field_reports — непустой массив
            var reports = root.GetProperty("field_reports");
            reports.GetArrayLength().Should().BeGreaterThan(0);
            reports[0].GetProperty("field_name").GetString().Should().Be("description");
        }

        // ── чужой товар → 404 (чтение скрывает существование) ──
        [Fact(DisplayName = "view_others_product_returns_404")]
        public async Task view_others_product_returns_404()
        {
            var ownerId = Guid.NewGuid();
            var productId = await CreateProductAsync(AuthClient(ownerId));

            var attacker = AuthClient(Guid.NewGuid());
            var resp = await attacker.GetAsync($"/api/v1/products/{productId}");

            resp.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "чужой товар при чтении не раскрывается (404, не 403)");
        }

        // ── несуществующий → 404 ──
        [Fact(DisplayName = "view_nonexistent_returns_404")]
        public async Task view_nonexistent_returns_404()
        {
            var client = AuthClient(Guid.NewGuid());
            var resp = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
