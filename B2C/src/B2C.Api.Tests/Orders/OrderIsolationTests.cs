using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;

using FluentAssertions;

namespace B2C.Api.Tests.Orders
{
    [Collection("Sequential")]
    public sealed class OrderIsolationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public OrderIsolationTests(CustomWebApplicationFactory factory)
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

        /// <summary>
        /// US-ORD-02 ключевой acceptance: попытка открыть чужой заказ возвращает 404 (не 403),
        /// чтобы не раскрывать факт существования.
        /// </summary>
        [Fact(DisplayName = "other_user_order_returns_404_not_403")]
        public async Task get_other_user_order_returns_404()
        {
            // Заказ создал buyer A.
            var ownerBuyerId = Guid.NewGuid();
            var ownerClient = CreateAuthorizedClient(ownerBuyerId);
            var orderId = await CreateOrderAsync(ownerClient);

            // Buyer B пытается прочитать.
            var attackerBuyerId = Guid.NewGuid();
            var attackerClient = CreateAuthorizedClient(attackerBuyerId);

            var resp = await attackerClient.GetAsync($"/api/v1/orders/{orderId}");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> GET OTHER: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "404 НЕ 403 — не раскрываем существование чужого ресурса");
            resp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// US-ORD-02: владелец видит свой заказ нормально.
        /// </summary>
        [Fact(DisplayName = "owner_gets_own_order")]
        public async Task get_own_order_returns_200_with_details()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var orderId = await CreateOrderAsync(client);

            var resp = await client.GetAsync($"/api/v1/orders/{orderId}");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> GET OWN: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var root = JsonDocument.Parse(body).RootElement;
            root.GetProperty("id").GetGuid().Should().Be(orderId);
            root.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        }

        /// <summary>
        /// US-ORD-02: список заказов возвращает ТОЛЬКО заказы текущего покупателя.
        /// </summary>
        [Fact(DisplayName = "list_orders_isolates_by_buyer")]
        public async Task list_returns_only_own_orders()
        {
            // Buyer A создаёт заказ.
            var buyerAId = Guid.NewGuid();
            var buyerAClient = CreateAuthorizedClient(buyerAId);
            var orderAId = await CreateOrderAsync(buyerAClient);

            // Buyer B создаёт свой заказ.
            var buyerBId = Guid.NewGuid();
            var buyerBClient = CreateAuthorizedClient(buyerBId);
            await CreateOrderAsync(buyerBClient);

            // Buyer A видит ровно свой один заказ.
            var resp = await buyerAClient.GetAsync("/api/v1/orders");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> LIST A: {resp.StatusCode} BODY: {body}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var items = JsonDocument.Parse(body).RootElement.GetProperty("items");
            items.GetArrayLength().Should().Be(1);
            items[0].GetProperty("id").GetGuid().Should().Be(orderAId);
        }

        /// <summary>
        /// Anonymous (без JWT) → 401 на GET /orders.
        /// </summary>
        [Fact(DisplayName = "unauthorized_returns_401")]
        public async Task list_orders_without_jwt_returns_401()
        {
            var anonymousClient = _factory.CreateClient();

            var resp = await anonymousClient.GetAsync("/api/v1/orders");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ===== helper =====

        private async Task<Guid> CreateOrderAsync(HttpClient client)
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();

            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Test", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", 100_00, 0, null, true,
                Array.Empty<CharacteristicValue>()));

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                delivery_address = "Москва",
                items = new[] { new { sku_id = skuId, quantity = 1 } },
            };

            var resp = await client.PostAsJsonAsync("/api/v1/orders", body);
            var json = await resp.Content.ReadAsStringAsync();
            resp.StatusCode.Should().Be(HttpStatusCode.Created, json);
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
        }
    }
}
