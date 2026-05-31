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
namespace B2B.Api.Tests.Skus
{
    [Collection("Sequential")]
    public sealed class DeleteSkuTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DeleteSkuTests(CustomWebApplicationFactory factory) => _factory = factory;

        private HttpClient AuthClient(Guid sellerId)
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return c;
        }

        private async Task<Guid> CreateProductAsync(HttpClient client)
        {
            var body = new
            {
                category_id = TestData.CategoryId,
                title = "Del SKU P",
                description = "d",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            return JsonDocument.Parse(await (await client.PostAsJsonAsync("/api/v1/products", body))
                .Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        }

        private async Task<Guid> CreateSkuAsync(HttpClient client, Guid productId)
        {
            var body = new
            {
                product_id = productId,
                name = "S",
                price = 1000000,
                discount = 0,
                cost_price = 500000,
                article = "A",
                images = new[] { new { url = "/s3/s.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            return JsonDocument.Parse(await (await client.PostAsJsonAsync("/api/v1/skus", body))
                .Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        }

        private async Task<ProductStatus> GetStatusAsync(Guid productId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            return (await db.Products.AsNoTracking().FirstAsync(p => p.Id == productId)).Status;
        }

        // ── удаление SKU проходит ──
        [Fact(DisplayName = "delete_sku_succeeds")]
        public async Task delete_sku_succeeds()
        {
            var client = AuthClient(Guid.NewGuid());
            var pid = await CreateProductAsync(client);
            var sku1 = await CreateSkuAsync(client, pid);
            await CreateSkuAsync(client, pid);   // второй SKU, чтобы не сработал переход

            var resp = await client.DeleteAsync($"/api/v1/skus/{sku1}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var sku = await db.Skus.AsNoTracking().FirstAsync(s => s.Id == sku1);
            sku.Deleted.Should().BeTrue();
        }

        // ── SKU с активными резервами → 409 ──
        [Fact(DisplayName = "delete_sku_with_active_reserves_returns_409")]
        public async Task delete_sku_with_active_reserves_returns_409()
        {
            var client = AuthClient(Guid.NewGuid());
            var pid = await CreateProductAsync(client);
            var skuId = await CreateSkuAsync(client, pid);

            // выставляем резерв через домен
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var sku = await db.Skus.FirstAsync(s => s.Id == skuId);
                sku.IncreaseStock(10);
                sku.Reserve(3);
                await db.SaveChangesAsync();
            }

            var resp = await client.DeleteAsync($"/api/v1/skus/{skuId}");
            resp.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var json = await resp.Content.ReadAsStringAsync();
            JsonDocument.Parse(json).RootElement.GetProperty("code").GetString()
                .Should().Be("CONFLICT");
        }

        // ── последний SKU у ON_MODERATION → товар в CREATED ──
        [Fact(DisplayName = "last_sku_on_moderation_transitions_product_to_created")]
        public async Task last_sku_on_moderation_transitions_product_to_created()
        {
            var client = AuthClient(Guid.NewGuid());
            var pid = await CreateProductAsync(client);
            var skuId = await CreateSkuAsync(client, pid);   // первый SKU → ON_MODERATION

            (await GetStatusAsync(pid)).Should().Be(ProductStatus.OnModeration);

            var resp = await client.DeleteAsync($"/api/v1/skus/{skuId}");   // последний SKU
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            (await GetStatusAsync(pid)).Should().Be(ProductStatus.Created,
                "без SKU товар на модерации возвращается в CREATED");

            // Событие в Moderation: товар сошёл с модерации (US-12 fix)
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var evt = await db.OutboxMessages.AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.EventType == "product.removed_from_moderation.v1"
                    && m.AggregateId == pid
                    && m.Destination == "moderation");

            evt.Should().NotBeNull(
                "удаление последнего SKU у ON_MODERATION-товара должно уведомить Moderation");
        }

        // ── удаление SKU у HARD_BLOCKED → 403 ──
        [Fact(DisplayName = "delete_sku_hard_blocked_product_returns_403")]
        public async Task delete_sku_hard_blocked_product_returns_403()
        {
            var client = AuthClient(Guid.NewGuid());
            var pid = await CreateProductAsync(client);
            var skuId = await CreateSkuAsync(client, pid);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var product = await db.Products.Include(p => p.FieldReports).FirstAsync(p => p.Id == pid);
                product.HardBlock(
                    new BlockingReason(Guid.NewGuid(), "x"),
                    new List<(FieldReportTarget, Guid?, string)>(),
                    new List<Guid>(),
                    DateTime.UtcNow);
                await db.SaveChangesAsync();
            }

            var resp = await client.DeleteAsync($"/api/v1/skus/{skuId}");
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ── удаление SKU у MODERATED-товара с остатком → событие SKU_OUT_OF_STOCK ──
        [Fact(DisplayName = "sku_out_of_stock_event_on_moderated_product")]
        public async Task sku_out_of_stock_event_on_moderated_product()
        {
            var client = AuthClient(Guid.NewGuid());
            var pid = await CreateProductAsync(client);
            var sku1 = await CreateSkuAsync(client, pid);
            await CreateSkuAsync(client, pid);   // второй SKU, чтобы товар не ушёл в CREATED

            // товар → MODERATED, у удаляемого SKU остаток > 0
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var product = await db.Products.Include(p => p.FieldReports).FirstAsync(p => p.Id == pid);
                product.Approve();   // → Moderated
                var sku = await db.Skus.FirstAsync(s => s.Id == sku1);
                sku.IncreaseStock(5);   // active > 0
                await db.SaveChangesAsync();
            }

            var resp = await client.DeleteAsync($"/api/v1/skus/{sku1}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope2 = _factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<B2BDbContext>();
            var evt = await db2.OutboxMessages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.EventType == "sku.out_of_stock.v1"
                    && m.AggregateId == sku1);
            evt.Should().NotBeNull("удаление SKU из витрины MODERATED шлёт SKU_OUT_OF_STOCK");
        }

    }
}
