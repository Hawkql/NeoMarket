using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
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
            var client = _factory.CreateClient();
            var token = JwtTokenHelper.GenerateBuyerToken(buyerId);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        /// <summary>
        /// US-ORD-01: если B2B reserve не удался — заказ НЕ создаётся,
        /// возвращается 409 RESERVE_FAILED. БД остаётся чистой (all-or-nothing).
        /// </summary>
        [Fact(DisplayName = "reserve_fail_returns_409_no_order_created")]
        public async Task reserve_failure_returns_409_and_no_order_persists()
        {
            // Arrange.
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Phone", null, 500_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", 500_00, 0, null, true,
                Array.Empty<CharacteristicValue>()));

            // КРИТИЧНО: настраиваем фейк reserve на провал.
            _factory.ReservationFake.ReserveBehavior = ReserveBehavior.AlwaysFail;

            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                delivery_address = "Москва",
                items = new[] { new { sku_id = skuId, quantity = 5 } },
            };

            // Act.
            var resp = await client.PostAsJsonAsync("/api/v1/orders", body);
            var bodyStr = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> RESPONSE: {resp.StatusCode} BODY: {bodyStr}");

            // Assert — 409.
            resp.StatusCode.Should().Be(HttpStatusCode.Conflict);

            // БД — заказ не создан.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var anyOrder = await db.Orders.AsNoTracking().AnyAsync(o => o.BuyerId == buyerId);
            anyOrder.Should().BeFalse("при провале reserve заказ не должен сохраняться");

            // Reserve был вызван хотя бы раз (попытка была).
            _factory.ReservationFake.ReserveCalls.Should().NotBeEmpty();
        }

        /// <summary>
        /// US-ORD-01: items пустой → 400 INVALID_REQUEST (валидатор).
        /// </summary>
        [Fact(DisplayName = "empty_items_returns_400")]
        public async Task empty_items_returns_400()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                delivery_address = "Москва",
                items = Array.Empty<object>(),
            };

            var resp = await client.PostAsJsonAsync("/api/v1/orders", body);
            var bodyStr = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> RESPONSE: {resp.StatusCode} BODY: {bodyStr}");

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// SKU отсутствует в каталоге B2B → 400 INVALID_REQUEST
        /// (нельзя заказать несуществующее).
        /// </summary>
        [Fact(DisplayName = "unknown_sku_returns_400")]
        public async Task unknown_sku_returns_400()
        {
            // Каталог ПУСТ — не сидим ничего.
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                delivery_address = "Москва",
                items = new[] { new { sku_id = Guid.NewGuid(), quantity = 1 } },
            };

            var resp = await client.PostAsJsonAsync("/api/v1/orders", body);
            var bodyStr = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> RESPONSE: {resp.StatusCode} BODY: {bodyStr}");

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // И reserve не вызывался — проверка существования SKU отсекла раньше.
            _factory.ReservationFake.ReserveCalls.Should().BeEmpty(
                "проверка существования SKU должна сработать ДО вызова reserve");
        }
    }
}
