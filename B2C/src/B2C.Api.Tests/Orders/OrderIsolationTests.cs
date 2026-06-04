using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

        [Fact(DisplayName = "other_user_order_returns_404_not_403")]
        public async Task get_other_user_order_returns_404()
        {
            var ownerBuyerId = Guid.NewGuid();
            var ownerClient = CreateAuthorizedClient(ownerBuyerId);
            var orderId = await CreateOrderAsync(ownerClient, ownerBuyerId);

            var attackerBuyerId = Guid.NewGuid();
            var attackerClient = CreateAuthorizedClient(attackerBuyerId);

            var resp = await attackerClient.GetAsync($"/api/v1/orders/{orderId}");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> GET OTHER: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "404 НЕ 403 — не раскрываем существование чужого ресурса");
            resp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        }

        [Fact(DisplayName = "owner_gets_own_order")]
        public async Task get_own_order_returns_200_with_details()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var orderId = await CreateOrderAsync(client, buyerId);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
                var inDb = await db.Orders.AsNoTracking()
                    .Where(o => o.Id == orderId)
                    .Select(o => new { o.Id, o.BuyerId, o.Status })
                    .FirstOrDefaultAsync();
                Console.WriteLine($">>> DEBUG: orderId={orderId}, buyerId(test)={buyerId}");
                Console.WriteLine($">>> DEBUG inDb: {System.Text.Json.JsonSerializer.Serialize(inDb)}");
            }

            var resp = await client.GetAsync($"/api/v1/orders/{orderId}");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> GET OWN: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var root = JsonDocument.Parse(body).RootElement;
            root.GetProperty("id").GetGuid().Should().Be(orderId);
            root.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);

            // openapi OrderResponse required-поля.
            root.GetProperty("buyer_id").GetGuid().Should().Be(buyerId);
            root.GetProperty("subtotal").GetInt32().Should().BeGreaterThan(0);
            root.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
            root.GetProperty("address").ValueKind.Should().Be(JsonValueKind.Object,
                "address должен быть объектом по openapi");
            root.GetProperty("address").GetProperty("city").GetString().Should().Be("Moscow");
        }

        [Fact(DisplayName = "list_orders_isolates_by_buyer")]
        public async Task list_returns_only_own_orders()
        {
            var buyerAId = Guid.NewGuid();
            var buyerAClient = CreateAuthorizedClient(buyerAId);
            var orderAId = await CreateOrderAsync(buyerAClient, buyerAId);

            var buyerBId = Guid.NewGuid();
            var buyerBClient = CreateAuthorizedClient(buyerBId);
            await CreateOrderAsync(buyerBClient, buyerBId);

            var resp = await buyerAClient.GetAsync("/api/v1/orders");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> LIST A: {resp.StatusCode} BODY: {body}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var items = JsonDocument.Parse(body).RootElement.GetProperty("items");
            items.GetArrayLength().Should().Be(1);
            items[0].GetProperty("id").GetGuid().Should().Be(orderAId);

            // openapi: items — полный OrderResponse, обязательно с buyer_id и address.
            items[0].GetProperty("buyer_id").GetGuid().Should().Be(buyerAId);
            items[0].GetProperty("address").ValueKind.Should().Be(JsonValueKind.Object);
        }

        [Fact(DisplayName = "unauthorized_returns_401")]
        public async Task list_orders_without_jwt_returns_401()
        {
            var anonymousClient = _factory.CreateClient();
            var resp = await anonymousClient.GetAsync("/api/v1/orders");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ===== helpers =====

        private async Task<Guid> CreateOrderAsync(HttpClient client, Guid buyerId)
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();

            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Test", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", 100_00, 0, null, true, AvailableQuantity: 100,
                Array.Empty<CharacteristicValue>()));

            var addressId = await SeedAddressAsync(buyerId);

            var body = new
            {
                address_id = addressId,
                payment_method_id = Guid.NewGuid(),
                items = new[] { new { sku_id = skuId, quantity = 1 } },
            };

            var resp = await PostOrderAsync(client, Guid.NewGuid(), body);
            var json = await resp.Content.ReadAsStringAsync();
            resp.StatusCode.Should().Be(HttpStatusCode.Created, json);
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
        }

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