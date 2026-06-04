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
        // openapi: канал событий B2B.
        private const string EventsUrl = "/api/v1/b2b/events";

        private readonly CustomWebApplicationFactory _factory;

        public InboxIdempotencyTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
            _factory.ReservationFake.Reset();
        }

        /// <summary>
        /// HttpClient с X-Service-Key — межсервисная аутентификация B2B → B2C
        /// (политика ServiceOnly), не JWT покупателя.
        /// </summary>
        private HttpClient CreateServiceClient()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Service-Key", "test-incoming-service-key");
            return client;
        }

        /// <summary>
        /// US-ORD-04 ключевой acceptance: повторное событие с тем же idempotency_key
        /// не применяется дважды. Inbox-дедуп через PK на idempotency_key.
        /// 
        /// Сценарий:
        ///   1. Гость кладёт SKU в корзину.
        ///   2. B2B шлёт PRODUCT_BLOCKED → 202, cart_item помечается ProductBlocked.
        ///   3. B2B шлёт ТО ЖЕ событие повторно (тот же key) → 202, no-op.
        ///   4. cart_item помечен один раз, в inbox одна запись.
        /// </summary>
        [Fact(DisplayName = "duplicate_event_not_processed_twice")]
        public async Task event_with_same_idempotency_key_processed_once()
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            SeedCatalog(productId, skuId);

            const string sessionId = "guest-inbox-1";
            var guestClient = _factory.CreateClient();
            guestClient.DefaultRequestHeaders.Add("X-Session-Id", sessionId);

            var addResp = await guestClient.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuId, quantity = 2 });
            addResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var serviceClient = CreateServiceClient();
            var idempotencyKey = Guid.NewGuid();

            var eventBody = new
            {
                idempotency_key = idempotencyKey,
                event_type = "PRODUCT_BLOCKED",
                occurred_at = DateTime.UtcNow,
                payload = new { product_id = productId, reason = (string?)null },
            };

            // openapi: 202 Accepted.
            var first = await serviceClient.PostAsJsonAsync(EventsUrl, eventBody);
            first.StatusCode.Should().Be(HttpStatusCode.Accepted,
                await first.Content.ReadAsStringAsync());

            var second = await serviceClient.PostAsJsonAsync(EventsUrl, eventBody);
            second.StatusCode.Should().Be(HttpStatusCode.Accepted,
                "повторный приём — не ошибка, idempotent");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();

            var cart = await db.Carts.AsNoTracking()
                .FirstAsync(c => c.Owner.SessionId == sessionId);
            cart.Items.Should().HaveCount(1);
            cart.Items.Single().UnavailableReason.Should().Be(UnavailableReason.ProductBlocked);

            var inboxCount = await db.InboxMessages.AsNoTracking()
                .CountAsync(m => m.IdempotencyKey == idempotencyKey);
            inboxCount.Should().Be(1);
        }

        /// <summary>
        /// US-ORD-04: запрос БЕЗ X-Service-Key → 401.
        /// </summary>
        [Fact(DisplayName = "events_without_service_key_return_401")]
        public async Task event_without_service_key_returns_401()
        {
            var client = _factory.CreateClient();

            var resp = await client.PostAsJsonAsync(EventsUrl, new
            {
                idempotency_key = Guid.NewGuid(),
                event_type = "PRODUCT_BLOCKED",
                occurred_at = DateTime.UtcNow,
                payload = new { product_id = Guid.NewGuid() },
            });

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// SKU_OUT_OF_STOCK помечает конкретный sku_id, не product_id.
        /// Другие SKU того же продукта остаются available.
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
                skuTargetId, productId, "Red", 100_00, 0, null, true, AvailableQuantity: 100,
                Array.Empty<CharacteristicValue>()));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuOtherId, productId, "Blue", 100_00, 0, null, true, AvailableQuantity: 100,
                Array.Empty<CharacteristicValue>()));

            const string sessionId = "guest-sku-oos-1";
            var guestClient = _factory.CreateClient();
            guestClient.DefaultRequestHeaders.Add("X-Session-Id", sessionId);

            await guestClient.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuTargetId, quantity = 1 });
            await guestClient.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuOtherId, quantity = 1 });

            var serviceClient = CreateServiceClient();
            var resp = await serviceClient.PostAsJsonAsync(EventsUrl, new
            {
                idempotency_key = Guid.NewGuid(),
                event_type = "SKU_OUT_OF_STOCK",
                occurred_at = DateTime.UtcNow,
                payload = new
                {
                    product_id = productId,
                    sku_id = skuTargetId,
                    available_quantity = 0,
                },
            });
            resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var cart = await db.Carts.AsNoTracking()
                .FirstAsync(c => c.Owner.SessionId == sessionId);

            var targetItem = cart.Items.Single(i => i.SkuId == skuTargetId);
            var otherItem = cart.Items.Single(i => i.SkuId == skuOtherId);

            targetItem.UnavailableReason.Should().Be(UnavailableReason.OutOfStock);
            otherItem.UnavailableReason.Should().Be(UnavailableReason.None);
        }

        /// <summary>
        /// PRODUCT_HARD_BLOCKED — обрабатывается, корзины помечаются как при PRODUCT_BLOCKED.
        /// </summary>
        [Fact(DisplayName = "product_hard_blocked_marks_cart_items")]
        public async Task product_hard_blocked_processes_successfully()
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            SeedCatalog(productId, skuId);

            const string sessionId = "guest-hard-block-1";
            var guestClient = _factory.CreateClient();
            guestClient.DefaultRequestHeaders.Add("X-Session-Id", sessionId);
            await guestClient.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuId, quantity = 1 });

            var serviceClient = CreateServiceClient();
            var idempotencyKey = Guid.NewGuid();

            var resp = await serviceClient.PostAsJsonAsync(EventsUrl, new
            {
                idempotency_key = idempotencyKey,
                event_type = "PRODUCT_HARD_BLOCKED",
                occurred_at = DateTime.UtcNow,
                payload = new { product_id = productId, reason = "sanctions" },
            });
            resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var cart = await db.Carts.AsNoTracking()
                .FirstAsync(c => c.Owner.SessionId == sessionId);

            cart.Items.Single().UnavailableReason.Should().Be(UnavailableReason.ProductBlocked);

            (await db.InboxMessages.AsNoTracking().AnyAsync(m => m.IdempotencyKey == idempotencyKey))
                .Should().BeTrue();
        }

        /// <summary>
        /// SKU_BACK_IN_STOCK принимается (202) и регистрируется в Inbox.
        /// Корзины это событие не меняет (MVP — только Inbox + лог).
        /// </summary>
        [Fact(DisplayName = "sku_back_in_stock_accepted_and_registered")]
        public async Task sku_back_in_stock_accepted()
        {
            var serviceClient = CreateServiceClient();
            var idempotencyKey = Guid.NewGuid();

            var resp = await serviceClient.PostAsJsonAsync(EventsUrl, new
            {
                idempotency_key = idempotencyKey,
                event_type = "SKU_BACK_IN_STOCK",
                occurred_at = DateTime.UtcNow,
                payload = new
                {
                    product_id = Guid.NewGuid(),
                    sku_id = Guid.NewGuid(),
                    available_quantity = 42,
                },
            });
            resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            (await db.InboxMessages.AsNoTracking().AnyAsync(m => m.IdempotencyKey == idempotencyKey))
                .Should().BeTrue();
        }

        /// <summary>
        /// PRICE_CHANGED принимается (202) и регистрируется в Inbox.
        /// </summary>
        [Fact(DisplayName = "price_changed_accepted_and_registered")]
        public async Task price_changed_accepted()
        {
            var serviceClient = CreateServiceClient();
            var idempotencyKey = Guid.NewGuid();

            var resp = await serviceClient.PostAsJsonAsync(EventsUrl, new
            {
                idempotency_key = idempotencyKey,
                event_type = "PRICE_CHANGED",
                occurred_at = DateTime.UtcNow,
                payload = new
                {
                    product_id = Guid.NewGuid(),
                    sku_id = Guid.NewGuid(),
                    old_price = 500_00,
                    new_price = 400_00,
                },
            });
            resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            (await db.InboxMessages.AsNoTracking().AnyAsync(m => m.IdempotencyKey == idempotencyKey))
                .Should().BeTrue();
        }

        /// <summary>
        /// Неизвестный event_type → 400 INVALID_REQUEST.
        /// </summary>
        [Fact(DisplayName = "unknown_event_type_returns_400")]
        public async Task unknown_event_type_returns_400()
        {
            var serviceClient = CreateServiceClient();

            var resp = await serviceClient.PostAsJsonAsync(EventsUrl, new
            {
                idempotency_key = Guid.NewGuid(),
                event_type = "TOTALLY_MADE_UP_EVENT",
                occurred_at = DateTime.UtcNow,
                payload = new { product_id = Guid.NewGuid() },
            });

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ===== helpers =====

        private void SeedCatalog(Guid productId, Guid skuId)
        {
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Test", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", 100_00, 0, null, true, AvailableQuantity: 100,
                Array.Empty<CharacteristicValue>()));
        }
    }
}