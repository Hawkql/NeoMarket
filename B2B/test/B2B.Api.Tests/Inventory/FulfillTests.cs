using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using B2B.Api.Tests.Infrastructure;
using B2B.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;


namespace B2B.Api.Tests.Inventory
{
    [Collection("Sequential")]
    public sealed class FulfillTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string ServiceKey = "test-service-key";
        private readonly CustomWebApplicationFactory _factory;

        public FulfillTests(CustomWebApplicationFactory factory) => _factory = factory;

        private HttpClient SellerClient(Guid sellerId)
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return c;
        }

        private HttpClient ServiceClient()
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Add("X-Service-Key", ServiceKey);
            return c;
        }

        // SKU со стоком, затем зарезервировано reserveQty → reserved=reserveQty, active=stock-reserveQty
        private async Task<Guid> CreateSkuReservedAsync(int stock, int reserveQty)
        {
            var seller = SellerClient(Guid.NewGuid());
            var prod = new
            {
                category_id = TestData.CategoryId,
                title = "F P",
                description = "d",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var pid = JsonDocument.Parse(await (await seller.PostAsJsonAsync("/api/v1/products", prod))
                .Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
            var skuBody = new
            {
                product_id = pid,
                name = "S",
                price = 1000000,
                discount = 0,
                cost_price = 500000,
                article = "A",
                images = new[] { new { url = "/s3/s.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var sid = JsonDocument.Parse(await (await seller.PostAsJsonAsync("/api/v1/skus", skuBody))
                .Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var sku = await db.Skus.FirstAsync(s => s.Id == sid);
                sku.IncreaseStock(stock);
                sku.Reserve(reserveQty);
                await db.SaveChangesAsync();
            }
            return sid;
        }

        private async Task<(int active, int reserved)> GetQtyAsync(Guid skuId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var sku = await db.Skus.AsNoTracking().FirstAsync(s => s.Id == skuId);
            return (sku.ActiveQuantity, sku.ReservedQuantity);
        }

        // ── fulfill уменьшает reserved ──
        [Fact(DisplayName = "fulfill_decreases_reserved_quantity")]
        public async Task fulfill_decreases_reserved_quantity()
        {
            var sku = await CreateSkuReservedAsync(stock: 10, reserveQty: 4);  // active=6, reserved=4
            var orderId = Guid.NewGuid();

            var body = new { order_id = orderId, items = new[] { new { sku_id = sku, quantity = 4 } } };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/fulfill", body);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var (_, reserved) = await GetQtyAsync(sku);
            reserved.Should().Be(0, "fulfill списывает резерв");
        }

        // ── active_quantity не меняется ──
        [Fact(DisplayName = "active_quantity_unchanged")]
        public async Task active_quantity_unchanged()
        {
            var sku = await CreateSkuReservedAsync(stock: 10, reserveQty: 4);  // active=6
            var orderId = Guid.NewGuid();

            var body = new { order_id = orderId, items = new[] { new { sku_id = sku, quantity = 4 } } };
            (await ServiceClient().PostAsJsonAsync("/api/v1/inventory/fulfill", body))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            var (active, _) = await GetQtyAsync(sku);
            active.Should().Be(6, "fulfill не трогает доступный остаток");
        }

        // ── идемпотентность: повтор не списывает дважды ──
        [Fact(DisplayName = "idempotent_fulfill_no_double_deduction")]
        public async Task idempotent_fulfill_no_double_deduction()
        {
            var sku = await CreateSkuReservedAsync(stock: 10, reserveQty: 6);  // reserved=6
            var orderId = Guid.NewGuid();
            var body = new { order_id = orderId, items = new[] { new { sku_id = sku, quantity = 3 } } };

            (await ServiceClient().PostAsJsonAsync("/api/v1/inventory/fulfill", body))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await GetQtyAsync(sku)).reserved.Should().Be(3);

            // повтор того же order_id
            var dup = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/fulfill", body);
            dup.StatusCode.Should().Be(HttpStatusCode.OK);
            (await GetQtyAsync(sku)).reserved.Should().Be(3, "повтор не должен списывать второй раз");
        }

        // ── без X-Service-Key → 401 ──
        [Fact(DisplayName = "missing_service_key_returns_401")]
        public async Task missing_service_key_returns_401()
        {
            var sku = await CreateSkuReservedAsync(stock: 10, reserveQty: 4);
            var noKey = _factory.CreateClient();
            var body = new { order_id = Guid.NewGuid(), items = new[] { new { sku_id = sku, quantity = 4 } } };
            var resp = await noKey.PostAsJsonAsync("/api/v1/inventory/fulfill", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
