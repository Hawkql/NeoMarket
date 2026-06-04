using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
using B2C.Application.Orders.Commands.CompleteFulfill;
using B2C.Domain.Addresses;
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

        [Fact(DisplayName = "repeated_fulfill_idempotent")]
        public async Task repeated_fulfill_does_not_call_b2b_again()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var orderId = await CreateAndDeliverOrderAsync(client, buyerId);

            var initialFulfillCount = _factory.ReservationFake.FulfillCalls.Count;
            initialFulfillCount.Should().Be(1);

            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new CompleteFulfillCommand(orderId));

            _factory.ReservationFake.FulfillCalls.Count.Should().Be(initialFulfillCount);

            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
            order.Status.Should().Be(OrderStatus.Delivered);
            order.FulfillCompletedAt.Should().NotBeNull();
        }

        [Fact(DisplayName = "fulfill_failure_keeps_requires_fulfill_true")]
        public async Task fulfill_failure_leaves_order_pending_for_retry()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            _factory.ReservationFake.FulfillAlwaysFails = true;

            var orderId = await CreateAndDeliverOrderAsync(client, buyerId);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);

            order.Status.Should().Be(OrderStatus.Delivered);
            order.FulfillCompletedAt.Should().BeNull();
            order.RequiresFulfill.Should().BeTrue();

            _factory.ReservationFake.FulfillAlwaysFails = false;

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new CompleteFulfillCommand(orderId));

            using var scope2 = _factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<B2CDbContext>();
            var orderAfterRetry = await db2.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
            orderAfterRetry.FulfillCompletedAt.Should().NotBeNull();
        }

        /// <summary>
        /// Создаёт заказ через checkout (header Idempotency-Key + новое body),
        /// прогоняет через статусы до DELIVERED, дёргает fulfill.
        /// </summary>
        private async Task<Guid> CreateAndDeliverOrderAsync(HttpClient client, Guid buyerId)
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Test", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", 100_00, Discount: 0, ImageUrl: null,
                InStock: true, AvailableQuantity: 100, Characteristics: Array.Empty<CharacteristicValue>()));

            var addressId = await SeedAddressAsync(buyerId);

            var body = new
            {
                address_id = addressId,
                payment_method_id = Guid.NewGuid(),
                items = new[] { new { sku_id = skuId, quantity = 1 } },
            };
            var resp = await PostOrderAsync(client, Guid.NewGuid(), body);
            var json = await resp.Content.ReadAsStringAsync();
            resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created, json);
            var orderId = JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders.FirstAsync(o => o.Id == orderId);

            order.StartAssembling();
            order.StartDelivering();
            order.MarkAsDelivered();

            await db.SaveChangesAsync();

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new CompleteFulfillCommand(orderId));

            return orderId;
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