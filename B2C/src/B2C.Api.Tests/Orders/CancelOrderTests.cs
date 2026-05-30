using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
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

        /// <summary>
        /// US-ORD-03: cancel PAID-заказа с успешным unreserve → CANCELLED.
        /// Базовый happy-path отмены.
        /// </summary>
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

            var status = JsonDocument.Parse(body).RootElement.GetProperty("status").GetString();
            status.Should().Be("cancelled");

            _factory.ReservationFake.UnreserveCalls.Should().NotBeEmpty(
                "unreserve должен быть вызван при отмене PAID-заказа");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var dbStatus = await db.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => o.Status)
                .FirstAsync();
            dbStatus.Should().Be(OrderStatus.Cancelled);
        }

        /// <summary>
        /// US-ORD-03 ключевой acceptance: cancel заказа в статусе ASSEMBLING → 409.
        /// Domain.CanBeCancelled = false для ASSEMBLING.
        /// </summary>
        [Fact(DisplayName = "cancel_assembling_order_returns_409")]
        public async Task cancel_assembling_order_returns_409_with_code()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var (orderId, _) = await CreateOrderAsync(client, buyerId);

            // Переводим в ASSEMBLING напрямую через БД (имитация админ-операции).
            await TransitionOrderToAssemblingAsync(orderId);

            var resp = await client.PostAsync($"/api/v1/orders/{orderId}/cancel", content: null);
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CANCEL ASSEMBLING: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var code = JsonDocument.Parse(body).RootElement.GetProperty("code").GetString();
            code.Should().Be("CANCEL_NOT_ALLOWED");

            // КРИТИЧНО: unreserve не должен был вызываться — отказ ДО обращения к B2B.
            // Иначе мы бы напрасно дёргали B2B при заведомо невозможной отмене.
            _factory.ReservationFake.UnreserveCalls.Should().BeEmpty(
                "проверка статуса должна сработать ДО вызова unreserve");

            // Статус остался ASSEMBLING.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var dbStatus = await db.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => o.Status)
                .FirstAsync();
            dbStatus.Should().Be(OrderStatus.Assembling);
        }

        /// <summary>
        /// US-ORD-03: unreserve упал → заказ уходит в CANCEL_PENDING (не CANCELLED).
        /// Background-job потом доретраит.
        /// </summary>
        [Fact(DisplayName = "unreserve_failure_transitions_to_cancel_pending")]
        public async Task unreserve_failure_keeps_order_in_cancel_pending()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var (orderId, _) = await CreateOrderAsync(client, buyerId);

            // Настраиваем фейк: unreserve всегда падает.
            _factory.ReservationFake.UnreserveAlwaysFails = true;

            var resp = await client.PostAsync($"/api/v1/orders/{orderId}/cancel", content: null);
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CANCEL UNRESERVE FAIL: {resp.StatusCode} BODY: {body}");

            // Cancel возвращает 200 (мы успешно перевели в CANCEL_PENDING — это не ошибка).
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var status = JsonDocument.Parse(body).RootElement.GetProperty("status").GetString();
            status.Should().Be("cancel_pending");

            // В БД — CANCEL_PENDING + LastUnreserveAttemptAt проставлен.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders
                .AsNoTracking()
                .FirstAsync(o => o.Id == orderId);

            order.Status.Should().Be(OrderStatus.CancelPending);
            order.LastUnreserveAttemptAt.Should().NotBeNull(
                "LastUnreserveAttemptAt должен быть проставлен для throttling в retry-job");
        }

        /// <summary>
        /// US-ORD-03: попытка отмены чужого заказа → 404 (не 403) — IDOR-защита.
        /// </summary>
        [Fact(DisplayName = "cancel_other_user_order_returns_404")]
        public async Task cancel_other_user_order_returns_404()
        {
            var ownerBuyerId = Guid.NewGuid();
            var ownerClient = CreateAuthorizedClient(ownerBuyerId);
            var (orderId, _) = await CreateOrderAsync(ownerClient, ownerBuyerId);

            // Другой покупатель пытается отменить.
            var attackerBuyerId = Guid.NewGuid();
            var attackerClient = CreateAuthorizedClient(attackerBuyerId);

            var resp = await attackerClient.PostAsync($"/api/v1/orders/{orderId}/cancel", content: null);
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CANCEL OTHER: {resp.StatusCode} BODY: {body}");

            // 404, не 403 — не палим существование чужого заказа.
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ===== helpers =====

        /// <summary>Создаёт PAID-заказ через checkout и возвращает (orderId, skuId).</summary>
        private async Task<(Guid OrderId, Guid SkuId)> CreateOrderAsync(HttpClient client, Guid buyerId)
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

            var orderId = JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
            return (orderId, skuId);
        }

        /// <summary>
        /// Прямой перевод в ASSEMBLING через Domain-методы. В реальной системе это
        /// делает админ через AdminOrdersController — для теста имитируем напрямую,
        /// чтобы не зависеть от admin-аутентификации.
        /// </summary>
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
