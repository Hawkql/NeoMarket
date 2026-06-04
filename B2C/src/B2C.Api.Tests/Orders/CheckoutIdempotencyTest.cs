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
using B2C.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2C.Api.Tests.Orders
{
    [Collection("Sequential")]
    public sealed class CheckoutIdempotencyTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CheckoutIdempotencyTests(CustomWebApplicationFactory factory)
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
        /// Создаёт адрес покупателя в БД и возвращает его Id.
        /// Чекаут принимает только address_id, который должен принадлежать buyer'у (IDOR).
        /// </summary>
        private async Task<Guid> SeedAddressAsync(Guid buyerId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var address = Address.Create(buyerId, "Russia", "Moscow", "Tverskaya 1");
            db.Set<Address>().Add(address);
            await db.SaveChangesAsync();
            return address.Id;
        }

        /// <summary>
        /// Отправляет POST /api/v1/orders с заголовком Idempotency-Key и openapi-телом.
        /// </summary>
        private static Task<HttpResponseMessage> PostOrderAsync(
            HttpClient client, Guid idempotencyKey, object body)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
            {
                Content = JsonContent.Create(body),
            };
            req.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
            return client.SendAsync(req);
        }

        /// <summary>
        /// US-ORD-01: повторный POST с тем же Idempotency-Key — тот же заказ, без дубля.
        /// </summary>
        [Fact(DisplayName = "idempotency_returns_existing_order")]
        public async Task same_idempotency_key_returns_existing_order()
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            SeedCatalog(productId, skuId, price: 100_00, title: "Test Product", skuName: "Default");

            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var addressId = await SeedAddressAsync(buyerId);

            var idempotencyKey = Guid.NewGuid();
            var body = new
            {
                address_id = addressId,
                payment_method_id = Guid.NewGuid(),
                comment = (string?)null,
                items = new[] { new { sku_id = skuId, quantity = 2 } },
            };

            var firstResp = await PostOrderAsync(client, idempotencyKey, body);
            var firstBody = await firstResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> FIRST: {firstResp.StatusCode} BODY: {firstBody}");

            firstResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var firstOrderId = ExtractOrderId(firstBody);

            var secondResp = await PostOrderAsync(client, idempotencyKey, body);
            var secondBody = await secondResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> SECOND: {secondResp.StatusCode} BODY: {secondBody}");

            secondResp.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
            ExtractOrderId(secondBody).Should().Be(firstOrderId, "тот же ключ — тот же заказ");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var count = await db.Orders.AsNoTracking().CountAsync(o => o.BuyerId == buyerId);
            count.Should().Be(1, "idempotency не должна создавать дубли");
        }

        /// <summary>
        /// Цены в заказе фиксируются из B2B на сервере. После изменения цены в B2B
        /// в заказе остаётся snapshot (Order.Subtotal).
        /// </summary>
        [Fact(DisplayName = "prices_frozen_from_b2b")]
        public async Task order_unit_price_is_snapshot_from_b2b()
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            const int initialPrice = 500_00;
            SeedCatalog(productId, skuId, price: initialPrice, title: "Phone", skuName: "Black");

            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var addressId = await SeedAddressAsync(buyerId);

            var body = new
            {
                address_id = addressId,
                payment_method_id = Guid.NewGuid(),
                items = new[] { new { sku_id = skuId, quantity = 1 } },
            };

            var resp = await PostOrderAsync(client, Guid.NewGuid(), body);
            var bodyStr = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> RESPONSE: {resp.StatusCode} BODY: {bodyStr}");
            resp.StatusCode.Should().Be(HttpStatusCode.Created);

            // Имитируем изменение цены в каталоге.
            SeedCatalog(productId, skuId, price: 999_00, title: "Phone", skuName: "Black");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.BuyerId == buyerId);

            // openapi: subtotal = сумма по позициям копейками.
            order.Subtotal.Should().Be(initialPrice, "цена замораживается при создании заказа");
        }

        private void SeedCatalog(Guid productId, Guid skuId, int price, string title, string skuName)
        {
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, title, ImageUrl: null,
                Price: price, OldPrice: null, Discount: null,
                InStock: true, Rating: null, ReviewsCount: null));

            _factory.CatalogFake.SeedSku(new SkuInfo(
                Id: skuId,
                ProductId: productId,
                Name: skuName,
                Price: price,
                Discount: 0,
                ImageUrl: null,
                InStock: true,
                AvailableQuantity: 100,
                Characteristics: Array.Empty<CharacteristicValue>()));
        }

        private static Guid ExtractOrderId(string responseBody)
            => JsonDocument.Parse(responseBody).RootElement.GetProperty("id").GetGuid();
    }
}