using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
using B2C.Domain.Carts;
using B2C.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2C.Api.Tests.B2BEvents
{
    [Collection("Sequential")]
    public sealed class InboxIdempotencyTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public InboxIdempotencyTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
            _factory.ReservationFake.Reset();
        }

        /// <summary>
        /// Подготовка HttpClient с заголовком X-Service-Key — это межсервисная
        /// аутентификация B2B → B2C, не JWT покупателя.
        /// </summary>
        private HttpClient CreateServiceClient()
        {
            var client = _factory.CreateClient();
            // Этот ключ совпадает с ServiceKey:Incoming в CustomWebApplicationFactory.
            client.DefaultRequestHeaders.Add("X-Service-Key", "test-incoming-service-key");
            return client;
        }

        /// <summary>
        /// US-ORD-04 ключевой acceptance: повторное событие с тем же idempotency_key
        /// НЕ применяется дважды. Inbox-дедуп.
        /// 
        /// Сценарий:
        ///   1. Гость кладёт SKU в корзину.
        ///   2. B2B шлёт PRODUCT_BLOCKED → cart_item помечается ProductBlocked.
        ///   3. B2B шлёт ТО ЖЕ событие повторно (тот же idempotency_key) → 200, no-op.
        ///   4. cart_item остаётся в том же состоянии (один раз помечен).
        /// </summary>
        [Fact(DisplayName = "duplicate_event_not_processed_twice")]
        public async Task event_with_same_idempotency_key_processed_once()
        {
            // 1. Гость кладёт товар в корзину.
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Test", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", 100_00, Discount: 0, ImageUrl: null,
                InStock: true, Characteristics: Array.Empty<CharacteristicValue>()));

            const string sessionId = "guest-inbox-1";
            var guestClient = _factory.CreateClient();
            guestClient.DefaultRequestHeaders.Add("X-Session-Id", sessionId);

            var addResp = await guestClient.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuId, quantity = 2 });
            addResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 2. B2B шлёт PRODUCT_BLOCKED.
            var serviceClient = CreateServiceClient();
            var idempotencyKey = Guid.NewGuid();

            var eventBody = new
            {
                idempotency_key = idempotencyKey,
                event_type = "product_blocked",
                data = new
                {
                    product_id = productId,
                    sku_ids = new[] { skuId },
                },
            };

            var first = await serviceClient.PostAsJsonAsync("/api/v1/b2b/events", eventBody);
            first.StatusCode.Should().Be(HttpStatusCode.OK,
                await first.Content.ReadAsStringAsync());

            // 3. Повторное событие с тем же ключом.
            var second = await serviceClient.PostAsJsonAsync("/api/v1/b2b/events", eventBody);
            second.StatusCode.Should().Be(HttpStatusCode.OK,
                "повторное событие — не ошибка, idempotent");

            // 4. В БД — корзина имеет позицию, помеченную ProductBlocked, ОДИН раз.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();

            // Загружаем корзину гостя. Items — Owned-коллекция, EF подгружает автоматически.
            var cart = await db.Carts.AsNoTracking()
                .FirstAsync(c => c.Owner.SessionId == sessionId);

            cart.Items.Should().HaveCount(1);
            cart.Items.Single().UnavailableReason.Should().Be(UnavailableReason.ProductBlocked);

            // Inbox — ровно одна запись по этому idempotency_key (защита через PK).
            var inboxCount = await db.InboxMessages.AsNoTracking()
                .CountAsync(m => m.IdempotencyKey == idempotencyKey);
            inboxCount.Should().Be(1);
        }

        /// <summary>
        /// US-ORD-04: запрос БЕЗ X-Service-Key → 401, событие не обрабатывается.
        /// </summary>
        [Fact(DisplayName = "events_without_service_key_return_401")]
        public async Task event_without_service_key_returns_401()
        {
            var client = _factory.CreateClient();   // без X-Service-Key

            var resp = await client.PostAsJsonAsync("/api/v1/b2b/events", new
            {
                idempotency_key = Guid.NewGuid(),
                event_type = "product_blocked",
                data = new { product_id = Guid.NewGuid(), sku_ids = new[] { Guid.NewGuid() } },
            });

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// SKU_OUT_OF_STOCK: помечает конкретный sku_id, не product_id.
        /// </summary>
        [Fact(DisplayName = "sku_out_of_stock_marks_specific_sku_only")]
        public async Task sku_out_of_stock_event_marks_only_specific_sku()
        {
            var productId = Guid.NewGuid();
            var skuTargetId = Guid.NewGuid();
            var skuOtherId = Guid.NewGuid();

            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Phone", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuTargetId, productId, "Red", 100_00, Discount: 0, null, true, Array.Empty<CharacteristicValue>()));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuOtherId, productId, "Blue", 100_00, Discount: 0, null, true, Array.Empty<CharacteristicValue>()));

            const string sessionId = "guest-sku-oos-1";
            var guestClient = _factory.CreateClient();
            guestClient.DefaultRequestHeaders.Add("X-Session-Id", sessionId);

            await guestClient.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuTargetId, quantity = 1 });
            await guestClient.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuOtherId, quantity = 1 });

            var serviceClient = CreateServiceClient();
            await serviceClient.PostAsJsonAsync("/api/v1/b2b/events", new
            {
                idempotency_key = Guid.NewGuid(),
                event_type = "sku_out_of_stock",
                data = new { product_id = productId, sku_id = skuTargetId },
            });

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var cart = await db.Carts.AsNoTracking()
                .FirstAsync(c => c.Owner.SessionId == sessionId);

            var targetItem = cart.Items.Single(i => i.SkuId == skuTargetId);
            var otherItem = cart.Items.Single(i => i.SkuId == skuOtherId);

            targetItem.UnavailableReason.Should().Be(UnavailableReason.OutOfStock);
            otherItem.UnavailableReason.Should().Be(UnavailableReason.None,
                "другой SKU того же продукта не должен затрагиваться");
        }
    }
}