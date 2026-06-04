using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
using B2C.Domain.Addresses;
using B2C.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2C.Api.Tests.Orders
{
    [Collection("Sequential")]
    public sealed class CheckoutReserveFailTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CheckoutReserveFailTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
            _factory.ReservationFake.Reset();
        }

        private HttpClient CreateAuthorizedClient(Guid buyerId)
        {
            _factory.EnsureBuyer(buyerId);
            var client = _factory.CreateClient();
            var token = JwtTokenHelper.GenerateBuyerToken(buyerId);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact(DisplayName = "reserve_fail_returns_409_no_order_created")]
        public async Task reserve_failure_returns_409_and_no_order_persists()
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Phone", null, 500_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", 500_00, 0, null, true, AvailableQuantity: 100,
                Array.Empty<CharacteristicValue>()));

            _factory.ReservationFake.ReserveBehavior = ReserveBehavior.AlwaysFail;

            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var addressId = await SeedAddressAsync(buyerId);

            var body = new
            {
                address_id = addressId,
                payment_method_id = Guid.NewGuid(),
                items = new[] { new { sku_id = skuId, quantity = 5 } },
            };

            var resp = await PostOrderAsync(client, Guid.NewGuid(), body);
            var bodyStr = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> RESPONSE: {resp.StatusCode} BODY: {bodyStr}");

            resp.StatusCode.Should().Be(HttpStatusCode.Conflict);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var anyOrder = await db.Orders.AsNoTracking().AnyAsync(o => o.BuyerId == buyerId);
            anyOrder.Should().BeFalse("при провале reserve заказ не должен сохраняться");

            _factory.ReservationFake.ReserveCalls.Should().NotBeEmpty();
        }

        [Fact(DisplayName = "empty_items_returns_400")]
        public async Task empty_items_returns_400()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            // Валидатор отсечёт по Items.NotEmpty ДО handler'а — адрес seed-ить не нужно.
            // AddressId/PaymentMethodId присутствуют (NotEmpty валидатор пройдёт по ним).
            var body = new
            {
                address_id = Guid.NewGuid(),
                payment_method_id = Guid.NewGuid(),
                items = Array.Empty<object>(),
            };

            var resp = await PostOrderAsync(client, Guid.NewGuid(), body);
            var bodyStr = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> RESPONSE: {resp.StatusCode} BODY: {bodyStr}");

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "unknown_sku_returns_400")]
        public async Task unknown_sku_returns_400()
        {
            // Каталог ПУСТ — SKU не существует.
            // Адрес ДОЛЖЕН быть засеян: IDOR-проверка address_id идёт ДО SKU-check,
            // иначе тест получит 404 вместо ожидаемого 400.
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var addressId = await SeedAddressAsync(buyerId);

            var body = new
            {
                address_id = addressId,
                payment_method_id = Guid.NewGuid(),
                items = new[] { new { sku_id = Guid.NewGuid(), quantity = 1 } },
            };

            var resp = await PostOrderAsync(client, Guid.NewGuid(), body);
            var bodyStr = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> RESPONSE: {resp.StatusCode} BODY: {bodyStr}");

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            _factory.ReservationFake.ReserveCalls.Should().BeEmpty(
                "проверка существования SKU должна сработать ДО вызова reserve");
        }

        // ===== helpers =====

        private async Task<Guid> SeedAddressAsync(Guid buyerId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var address = Address.Create(buyerId, "Russia", "Moscow", "Tverskaya 1");
            db.Set<Address>().Add(address);
            await db.SaveChangesAsync();
            return address.Id;
        }

        private static async Task<HttpResponseMessage> PostOrderAsync(
            HttpClient client, Guid idempotencyKey, object body)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
            {
                Content = JsonContent.Create(body),
            };
            req.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
            return await client.SendAsync(req);
        }
    }
}