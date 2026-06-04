using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
using B2C.Domain.Addresses;
using B2C.Domain.Orders;
using B2C.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2C.Api.Tests.Orders
{
    [Collection("Sequential")]
    public sealed class CancelOrderTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CancelOrderTests(CustomWebApplicationFactory factory)
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

        [Fact(DisplayName = "cancel_paid_order_succeeds")]
        public async Task cancel_paid_order_returns_200_and_status_cancelled()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var (orderId, _) = await CreateOrderAsync(client, buyerId);

            var resp = await client.PostAsync($"/api/v1/orders/{orderId}/cancel", content: null);
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CANCEL: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            // openapi: status — UPPER_SNAKE_CASE.
            var status = JsonDocument.Parse(body).RootElement.GetProperty("status").GetString();
            status.Should().Be("CANCELLED");

            _factory.ReservationFake.UnreserveCalls.Should().NotBeEmpty();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var dbStatus = await db.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => o.Status)
                .FirstAsync();
            dbStatus.Should().Be(OrderStatus.Cancelled);
        }

        [Fact(DisplayName = "cancel_assembling_order_returns_409")]
        public async Task cancel_assembling_order_returns_409_with_code()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var (orderId, _) = await CreateOrderAsync(client, buyerId);

            await TransitionOrderToAssemblingAsync(orderId);

            var resp = await client.PostAsync($"/api/v1/orders/{orderId}/cancel", content: null);
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CANCEL ASSEMBLING: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var code = JsonDocument.Parse(body).RootElement.GetProperty("code").GetString();
            code.Should().Be("CANCEL_NOT_ALLOWED");

            _factory.ReservationFake.UnreserveCalls.Should().BeEmpty();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var dbStatus = await db.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => o.Status)
                .FirstAsync();
            dbStatus.Should().Be(OrderStatus.Assembling);
        }

        [Fact(DisplayName = "unreserve_failure_transitions_to_cancel_pending")]
        public async Task unreserve_failure_keeps_order_in_cancel_pending()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var (orderId, _) = await CreateOrderAsync(client, buyerId);

            _factory.ReservationFake.UnreserveAlwaysFails = true;

            var resp = await client.PostAsync($"/api/v1/orders/{orderId}/cancel", content: null);
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CANCEL UNRESERVE FAIL: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            // openapi: UPPER_SNAKE_CASE.
            var status = JsonDocument.Parse(body).RootElement.GetProperty("status").GetString();
            status.Should().Be("CANCEL_PENDING");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);

            order.Status.Should().Be(OrderStatus.CancelPending);
            order.LastUnreserveAttemptAt.Should().NotBeNull();
        }

        [Fact(DisplayName = "cancel_other_user_order_returns_404")]
        public async Task cancel_other_user_order_returns_404()
        {
            var ownerBuyerId = Guid.NewGuid();
            var ownerClient = CreateAuthorizedClient(ownerBuyerId);
            var (orderId, _) = await CreateOrderAsync(ownerClient, ownerBuyerId);

            var attackerBuyerId = Guid.NewGuid();
            var attackerClient = CreateAuthorizedClient(attackerBuyerId);

            var resp = await attackerClient.PostAsync($"/api/v1/orders/{orderId}/cancel", content: null);

            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ===== helpers =====

        /// <summary>
        /// Создаёт PAID-заказ через checkout по новому openapi-контракту:
        /// header Idempotency-Key + body { address_id, payment_method_id, items }.
        /// </summary>
        private async Task<(Guid OrderId, Guid SkuId)> CreateOrderAsync(HttpClient client, Guid buyerId)
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

            var orderId = JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
            return (orderId, skuId);
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

        private async Task TransitionOrderToAssemblingAsync(Guid orderId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders.FirstAsync(o => o.Id == orderId);
            order.StartAssembling();
            await db.SaveChangesAsync();
        }
    }
}