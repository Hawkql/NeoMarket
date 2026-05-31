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
    public sealed class ReserveTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string ServiceKey = "test-service-key";
        private readonly CustomWebApplicationFactory _factory;

        public ReserveTests(CustomWebApplicationFactory factory) => _factory = factory;

        private HttpClient SellerClient(Guid sellerId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return client;
        }

        private HttpClient ServiceClient()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Service-Key", ServiceKey);
            return client;
        }

        private async Task<Guid> CreateSkuWithStockAsync(int stock)
        {
            var seller = SellerClient(Guid.NewGuid());
            var prodBody = new
            {
                category_id = TestData.CategoryId,
                title = "Inv P",
                description = "d",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var pid = JsonDocument.Parse(await (await seller.PostAsJsonAsync("/api/v1/products", prodBody))
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

            // выставляем сток через домен
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var sku = await db.Skus.FirstAsync(s => s.Id == sid);
            sku.IncreaseStock(stock);
            await db.SaveChangesAsync();
            return sid;
        }

        private async Task<int> GetActiveAsync(Guid skuId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var sku = await db.Skus.AsNoTracking().FirstAsync(s => s.Id == skuId);
            return sku.ActiveQuantity;
        }

        private async Task<int> GetReservedAsync(Guid skuId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var sku = await db.Skus.AsNoTracking().FirstAsync(s => s.Id == skuId);
            return sku.ReservedQuantity;
        }

        // ── резерв всех SKU проходит ──
        [Fact(DisplayName = "reserve_all_skus_succeeds")]
        public async Task reserve_all_skus_succeeds()
        {
            var sku1 = await CreateSkuWithStockAsync(10);
            var sku2 = await CreateSkuWithStockAsync(5);

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                order_id = Guid.NewGuid(),
                items = new[]
                {
                    new { sku_id = sku1, quantity = 3 },
                    new { sku_id = sku2, quantity = 2 }
                }
            };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/reserve", body);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            (await GetActiveAsync(sku1)).Should().Be(7);
            (await GetReservedAsync(sku1)).Should().Be(3);
            (await GetActiveAsync(sku2)).Should().Be(3);
            (await GetReservedAsync(sku2)).Should().Be(2);
        }

        // ── нехватка одного → 409, всё откатывается ──
        [Fact(DisplayName = "partial_insufficient_stock_returns_409_all_rollback")]
        public async Task partial_insufficient_stock_returns_409_all_rollback()
        {
            var ok = await CreateSkuWithStockAsync(10);
            var low = await CreateSkuWithStockAsync(1);   // не хватит на 5

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                order_id = Guid.NewGuid(),
                items = new[]
                {
                    new { sku_id = ok, quantity = 3 },
                    new { sku_id = low, quantity = 5 }   // провал
                }
            };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/reserve", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Conflict);

            // откат: первый SKU не тронут
            (await GetActiveAsync(ok)).Should().Be(10, "all-or-nothing: ничего не списано");
            (await GetReservedAsync(ok)).Should().Be(0);
        }

        // ── идемпотентность: повтор не списывает дважды ──
        [Fact(DisplayName = "idempotent_reserve_returns_200_without_double_deduction")]
        public async Task idempotent_reserve_returns_200_without_double_deduction()
        {
            var sku = await CreateSkuWithStockAsync(10);
            var key = Guid.NewGuid();
            var body = new
            {
                idempotency_key = key,
                order_id = Guid.NewGuid(),
                items = new[] { new { sku_id = sku, quantity = 4 } }
            };

            var first = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/reserve", body);
            first.StatusCode.Should().Be(HttpStatusCode.OK);
            (await GetActiveAsync(sku)).Should().Be(6);

            // повтор с тем же ключом
            var second = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/reserve", body);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            (await GetActiveAsync(sku)).Should().Be(6, "повтор не должен списывать второй раз");
        }

        // ── при ActiveQuantity=0 шлётся событие SKU_OUT_OF_STOCK ──
        [Fact(DisplayName = "sku_out_of_stock_event_emitted")]
        public async Task sku_out_of_stock_event_emitted()
        {
            var sku = await CreateSkuWithStockAsync(5);
            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                order_id = Guid.NewGuid(),
                items = new[] { new { sku_id = sku, quantity = 5 } }   // обнуляет active
            };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/reserve", body);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            (await GetActiveAsync(sku)).Should().Be(0);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var evt = await db.OutboxMessages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.EventType == "sku.out_of_stock.v1");
            evt.Should().NotBeNull("при обнулении остатка шлётся SKU_OUT_OF_STOCK");
        }

        // ── unreserve возвращает количества ──
        [Fact(DisplayName = "unreserve_restores_quantities")]
        public async Task unreserve_restores_quantities()
        {
            var sku = await CreateSkuWithStockAsync(10);
            var orderId = Guid.NewGuid();

            var reserveBody = new
            {
                idempotency_key = Guid.NewGuid(),
                order_id = orderId,
                items = new[] { new { sku_id = sku, quantity = 4 } }
            };
            (await ServiceClient().PostAsJsonAsync("/api/v1/inventory/reserve", reserveBody))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await GetActiveAsync(sku)).Should().Be(6);

            var unreserveBody = new
            {
                order_id = orderId,
                items = new[] { new { sku_id = sku, quantity = 4 } }
            };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/unreserve", unreserveBody);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            (await GetActiveAsync(sku)).Should().Be(10, "unreserve возвращает остаток");
            (await GetReservedAsync(sku)).Should().Be(0);
        }
        // ── unreserve должен брать количество из сохранённого резерва, а не из тела ──
        // ── unreserve должен брать количество из сохранённого резерва, а не из тела ──
        [Fact(DisplayName = "unreserve_uses_stored_reservation_not_request_body")]
        public async Task unreserve_uses_stored_reservation_not_request_body()
        {
            var skuId = await CreateSkuWithStockAsync(10);
            var orderId = Guid.NewGuid();

            var reserveBody = new
            {
                idempotency_key = Guid.NewGuid(),
                order_id = orderId,
                items = new[] { new { sku_id = skuId, quantity = 3 } }
            };
            (await ServiceClient().PostAsJsonAsync("/api/v1/inventory/reserve", reserveBody))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            // Атакующий пытается прислать quantity=10 для того же order_id
            var maliciousUnreserve = new
            {
                order_id = orderId,
                items = new[] { new { sku_id = skuId, quantity = 10 } }   // больше, чем зарезервировано
            };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/unreserve", maliciousUnreserve);
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest, "тело не совпадает с сохранённым резервом");

            // SKU не должен быть тронут после неуспешного unreserve
            (await GetActiveAsync(skuId)).Should().Be(7);     // 10 - 3 (только reserve)
            (await GetReservedAsync(skuId)).Should().Be(3);
        }

        // ── повторный unreserve / неизвестный order_id → 200 (идемпотентность) ──
        [Fact(DisplayName = "unreserve_with_unknown_order_returns_200")]
        public async Task unreserve_with_unknown_order_returns_200()
        {
            var unknownOrder = Guid.NewGuid();
            var body = new
            {
                order_id = unknownOrder,
                items = new[] { new { sku_id = Guid.NewGuid(), quantity = 1 } }
            };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/unreserve", body);
            resp.StatusCode.Should().Be(HttpStatusCode.OK, "unreserve без резерва — идемпотентно 200");
        }

        // ── unreserve с quantity, не совпадающим с сохранённым → 400 ──
        [Fact(DisplayName = "unreserve_quantity_mismatch_returns_400")]
        public async Task unreserve_quantity_mismatch_returns_400()
        {
            var skuId = await CreateSkuWithStockAsync(5);
            var orderId = Guid.NewGuid();

            var reserve = new
            {
                idempotency_key = Guid.NewGuid(),
                order_id = orderId,
                items = new[] { new { sku_id = skuId, quantity = 2 } }
            };
            (await ServiceClient().PostAsJsonAsync("/api/v1/inventory/reserve", reserve))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            // Передаём quantity, не совпадающий с сохранённым
            var mismatch = new
            {
                order_id = orderId,
                items = new[] { new { sku_id = skuId, quantity = 1 } }   // в БД лежит 2
            };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/inventory/unreserve", mismatch);
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await resp.Content.ReadAsStringAsync();
            JsonDocument.Parse(body).RootElement.GetProperty("code").GetString()
                .Should().Be("INVALID_REQUEST");
        }
    }
}
