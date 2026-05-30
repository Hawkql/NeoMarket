using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
using B2C.Application.Orders.Commands.CompleteFulfill;
using B2C.Domain.Orders;
using B2C.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2C.Api.Tests.Orders
{
    [Collection("Sequential")]
    public sealed class FulfillIdempotencyTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public FulfillIdempotencyTests(CustomWebApplicationFactory factory)
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
        /// US-ORD-05 ключевой acceptance: повторный вызов fulfill для того же заказа —
        /// 200 без побочных эффектов. На стороне B2C: FulfillCompletedAt не меняется,
        /// fulfill в B2B не вызывается дважды (наш handler — no-op при RequiresFulfill=false).
        /// </summary>
        [Fact(DisplayName = "repeated_fulfill_idempotent")]
        public async Task repeated_fulfill_does_not_call_b2b_again()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var orderId = await CreateAndDeliverOrderAsync(client, buyerId);

            // На момент перевода в DELIVERED — fulfill уже вызвался (через OrderDeliveredEvent).
            var initialFulfillCount = _factory.ReservationFake.FulfillCalls.Count;
            initialFulfillCount.Should().Be(1, "fulfill должен сработать ровно один раз при DELIVERED");

            // Эмулируем повторный вызов — например, retry-job (или второй раз handler).
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new CompleteFulfillCommand(orderId));

            // Fulfill в B2B НЕ должен вызваться повторно — handler проверяет RequiresFulfill.
            _factory.ReservationFake.FulfillCalls.Count.Should().Be(initialFulfillCount,
                "повторный CompleteFulfillCommand не должен дёргать B2B (handler no-op)");

            // В БД — заказ в DELIVERED, FulfillCompletedAt проставлен один раз.
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
            order.Status.Should().Be(OrderStatus.Delivered);
            order.FulfillCompletedAt.Should().NotBeNull();
        }

        /// <summary>
        /// US-ORD-05: если fulfill упал — заказ остаётся DELIVERED + RequiresFulfill=true,
        /// retry потом доделает.
        /// </summary>
        [Fact(DisplayName = "fulfill_failure_keeps_requires_fulfill_true")]
        public async Task fulfill_failure_leaves_order_pending_for_retry()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            // Настраиваем фейк fulfill на падение ДО создания заказа.
            _factory.ReservationFake.FulfillAlwaysFails = true;

            var orderId = await CreateAndDeliverOrderAsync(client, buyerId);

            // Заказ DELIVERED, но fulfill не завершён.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);

            order.Status.Should().Be(OrderStatus.Delivered);
            order.FulfillCompletedAt.Should().BeNull(
                "FulfillCompletedAt не проставлен при провале — retry-job доделает");
            order.RequiresFulfill.Should().BeTrue();

            // Теперь fulfill работает — имитируем retry.
            _factory.ReservationFake.FulfillAlwaysFails = false;

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new CompleteFulfillCommand(orderId));

            // Свежий scope, чтобы видеть зафиксированное состояние.
            using var scope2 = _factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<B2CDbContext>();
            var orderAfterRetry = await db2.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
            orderAfterRetry.FulfillCompletedAt.Should().NotBeNull(
                "после успешного retry FulfillCompletedAt проставляется");
        }

        /// <summary>
        /// Создаёт заказ через checkout, переводит его через все статусы до DELIVERED.
        /// Возвращает orderId. После вызова event OrderDeliveredEvent уже опубликован,
        /// fulfill в B2B вызван (если не настроен fail).
        /// </summary>
        private async Task<Guid> CreateAndDeliverOrderAsync(HttpClient client, Guid buyerId)
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Test", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", 100_00, Discount: 0, ImageUrl: null,
                InStock: true, Characteristics: Array.Empty<CharacteristicValue>()));

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                delivery_address = "Москва",
                items = new[] { new { sku_id = skuId, quantity = 1 } },
            };
            var resp = await client.PostAsJsonAsync("/api/v1/orders", body);
            var json = await resp.Content.ReadAsStringAsync();
            var orderId = JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();

            // Прогоняем через статусы напрямую через Domain (имитируем админ-операции).
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders.FirstAsync(o => o.Id == orderId);

            order.StartAssembling();
            order.StartDelivering();
            order.MarkAsDelivered();  // ← поднимет OrderDeliveredEvent

            await db.SaveChangesAsync();

            // ВАЖНО: SaveChangesAsync здесь напрямую через DbContext, в обход TransactionManager,
            // поэтому MediatR.Publish domain events НЕ выполнится автоматически.
            // Для срабатывания fulfill вызываем CompleteFulfillCommand вручную.
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new CompleteFulfillCommand(orderId));

            return orderId;
        }
    }
}