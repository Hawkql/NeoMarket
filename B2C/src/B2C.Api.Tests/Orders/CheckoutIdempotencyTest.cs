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
using B2C.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        /// US-ORD-01: повторный POST с тем же idempotency_key возвращает существующий заказ,
        /// без создания дубля. reserve в B2B вызывается один раз (или вернёт идемпотентный
        /// результат на второй вызов — в нашем фейке кэшируется по ключу).
        /// </summary>
        [Fact(DisplayName = "idempotency_returns_existing_order")]
        public async Task same_idempotency_key_returns_existing_order()
        {
            // Arrange — каталог: один товар, один SKU.
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            SeedCatalog(productId, skuId, price: 100_00, title: "Test Product", skuName: "Default");

            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var idempotencyKey = Guid.NewGuid();
            var body = new
            {
                idempotency_key = idempotencyKey,
                delivery_address = "Москва, Тверская 1",
                items = new[] { new { sku_id = skuId, quantity = 2 } },
            };

            // Act — первый submit.
            var firstResp = await client.PostAsJsonAsync("/api/v1/orders", body);
            var firstBody = await firstResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> FIRST: {firstResp.StatusCode} BODY: {firstBody}");

            firstResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var firstOrderId = ExtractOrderId(firstBody);

            // Act — повторный submit с тем же idempotency_key.
            var secondResp = await client.PostAsJsonAsync("/api/v1/orders", body);
            var secondBody = await secondResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> SECOND: {secondResp.StatusCode} BODY: {secondBody}");

            // Assert — тот же заказ.
            secondResp.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
            var secondOrderId = ExtractOrderId(secondBody);
            secondOrderId.Should().Be(firstOrderId, "idempotency_key должен вернуть тот же заказ");

            // В БД — ровно один заказ под этим ключом.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var count = await db.Orders
                .AsNoTracking()
                .CountAsync(o => o.BuyerId == buyerId);
            count.Should().Be(1, "idempotency не должна создавать дубли");
        }

        /// <summary>
        /// Цены в заказе фиксируются из B2B на сервере, не принимаются от клиента.
        /// Даже если потом цена в каталоге изменится, в заказе остаётся snapshot.
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

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                delivery_address = "Москва",
                items = new[] { new { sku_id = skuId, quantity = 1 } },
            };

            var resp = await client.PostAsJsonAsync("/api/v1/orders", body);
            var bodyStr = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> RESPONSE: {resp.StatusCode} BODY: {bodyStr}");
            resp.StatusCode.Should().Be(HttpStatusCode.Created);

            // Имитируем изменение цены в каталоге после создания заказа.
            SeedCatalog(productId, skuId, price: 999_00, title: "Phone", skuName: "Black");

            // Читаем заказ из БД — цена должна остаться initialPrice.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var order = await db.Orders
                .AsNoTracking()
                .FirstAsync(o => o.BuyerId == buyerId);

            order.TotalAmount.Should().Be(initialPrice, "цена замораживается при создании заказа");
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
                Characteristics: Array.Empty<CharacteristicValue>()));
        }

        private static Guid ExtractOrderId(string responseBody)
            => JsonDocument.Parse(responseBody).RootElement.GetProperty("id").GetGuid();
    }
}
