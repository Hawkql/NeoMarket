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
    /// <summary>
    /// US-B2B-03: редактирование товара/SKU.
    /// MODERATED/BLOCKED → ON_MODERATION + событие EDITED;
    /// HARD_BLOCKED → 403; чужой товар → 403 NOT_OWNER; резервы SKU сохраняются.
    /// </summary>
    [Collection("Sequential")]
    public sealed class EditProductTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EditProductTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

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
                title = "Test Product",
                description = "desc",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var resp = await client.PostAsJsonAsync("/api/v1/products", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
        }

        private async Task<Guid> CreateSkuAsync(HttpClient client, Guid productId)
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
            var resp = await client.PostAsJsonAsync("/api/v1/skus", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
        }

        // Доводит товар до OnModeration (через первый SKU), затем Approve → Moderated
        private async Task DriveToModeratedAsync(Guid productId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var product = await db.Products
                .Include(p => p.FieldReports)
                .FirstAsync(p => p.Id == productId);
            product.Approve();   // OnModeration → Moderated
            await db.SaveChangesAsync();
        }

        // Доводит товар до Blocked (OnModeration → Block)
        private async Task DriveToBlockedAsync(Guid productId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var product = await db.Products
                .Include(p => p.FieldReports)
                .FirstAsync(p => p.Id == productId);
            product.Block(
                new BlockingReason(Guid.NewGuid(), "reason", "comment"),
                new List<(FieldReportTarget, Guid?, string)>(),
                new List<Guid>(),
                DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        // ── MODERATED → правка → ON_MODERATION + событие EDITED ──
        [Fact(DisplayName = "edit_moderated_product_returns_to_on_moderation")]
        public async Task edit_moderated_product_returns_to_on_moderation()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);
            await CreateSkuAsync(client, productId);     // → OnModeration
            await DriveToModeratedAsync(productId);       // → Moderated

            var editBody = new { title = "Updated Title", description = "Updated desc" };
            var resp = await client.PutAsJsonAsync($"/api/v1/products/{productId}", editBody);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();

            var product = await db.Products.AsNoTracking().FirstAsync(p => p.Id == productId);
            product.Status.Should().Be(ProductStatus.OnModeration,
                "правка MODERATED-товара возвращает его на повторную модерацию");

            // Событие EDITED (sent_to_moderation) записано
            var evt = await db.OutboxMessages.AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.EventType == "product.sent_to_moderation.v1"
                    && m.AggregateId == productId
                    && m.Payload.Contains("edited"));
            evt.Should().NotBeNull("правка должна породить событие EDITED");
        }

        // ── BLOCKED → правка → ON_MODERATION ──
        [Fact(DisplayName = "edit_blocked_product_returns_to_on_moderation")]
        public async Task edit_blocked_product_returns_to_on_moderation()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);
            await CreateSkuAsync(client, productId);     // → OnModeration
            await DriveToBlockedAsync(productId);         // → Blocked

            var editBody = new { title = "Fixed Title", description = "Fixed desc" };
            var resp = await client.PutAsJsonAsync($"/api/v1/products/{productId}", editBody);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var product = await db.Products.AsNoTracking().FirstAsync(p => p.Id == productId);
            product.Status.Should().Be(ProductStatus.OnModeration,
                "правка BLOCKED-товара возвращает его на повторную модерацию");
        }

        // ── правка SKU не сбрасывает резервы ──
        [Fact(DisplayName = "reserves_preserved_after_sku_edit")]
        public async Task reserves_preserved_after_sku_edit()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);
            var skuId = await CreateSkuAsync(client, productId);

            // Выставляем сток и резерв напрямую через домен
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var sku = await db.Skus.FirstAsync(s => s.Id == skuId);
                sku.IncreaseStock(10);
                sku.Reserve(3);          // ReservedQuantity = 3, ActiveQuantity = 7
                await db.SaveChangesAsync();
            }

            // Редактируем SKU (цена/имя) — резерв трогать не должны
            var editBody = new { name = "Renamed SKU", price = 2000000 };
            var resp = await client.PutAsJsonAsync($"/api/v1/skus/{skuId}", editBody);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope2 = _factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<B2BDbContext>();
            var updated = await db2.Skus.AsNoTracking().FirstAsync(s => s.Id == skuId);

            updated.ReservedQuantity.Should().Be(3, "резерв сохраняется при правке SKU");
            updated.ActiveQuantity.Should().Be(7, "доступный остаток не меняется при правке");
        }

        // ── HARD_BLOCKED → правка → 403 ──
        [Fact(DisplayName = "edit_hard_blocked_returns_403")]
        public async Task edit_hard_blocked_returns_403()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var product = await db.Products
                    .Include(p => p.FieldReports)
                    .FirstAsync(p => p.Id == productId);
                product.HardBlock(
                    new BlockingReason(Guid.NewGuid(), "bad", "x"),
                    new List<(FieldReportTarget, Guid?, string)>(),
                    new List<Guid>(),
                    DateTime.UtcNow);
                await db.SaveChangesAsync();
            }

            var editBody = new { title = "try edit", description = "try" };
            var resp = await client.PutAsJsonAsync($"/api/v1/products/{productId}", editBody);
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ── чужой товар → 403 NOT_OWNER ──
        [Fact(DisplayName = "edit_others_product_returns_403")]
        public async Task edit_others_product_returns_403()
        {
            var ownerId = Guid.NewGuid();
            var ownerClient = AuthClient(ownerId);
            var productId = await CreateProductAsync(ownerClient);

            // Другой продавец пытается редактировать
            var attackerId = Guid.NewGuid();
            var attackerClient = AuthClient(attackerId);

            var editBody = new { title = "hijack", description = "hijack" };
            var resp = await attackerClient.PutAsJsonAsync($"/api/v1/products/{productId}", editBody);

            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "редактирование чужого товара запрещено (NOT_OWNER)");

            var json = await resp.Content.ReadAsStringAsync();
            JsonDocument.Parse(json).RootElement.GetProperty("code").GetString()
                .Should().Be("NOT_OWNER");
        }
    }
}
