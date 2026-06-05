using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using B2B.Api.Tests.Infrastructure;
using B2B.Domain.Products;
using B2B.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2B.Api.Tests.Skus
{
    [Collection("Sequential")]
    public sealed class CreateSkuTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CreateSkuTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient AuthClient(Guid sellerId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return client;
        }

        // Создаёт товар через API, возвращает его id
        private async Task<Guid> CreateProductAsync(HttpClient client)
        {
            var body = new
            {
                category_id = TestData.CategoryId,
                title = "Test Product",
                description = "desc",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var resp = await client.PostAsJsonAsync("/api/v1/products", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
        }

        private static object ValidSkuBody(Guid productId) => new
        {
            product_id = productId,
            name = "Black 256GB",
            price = 9999900,
            discount = 0,
            cost_price = 5000000,
            article = "ART-1",
            images = new[] { new { url = "/s3/sku.jpg", ordering = 0 } },
            characteristics = new[] { new { name = "Цвет", value = "Чёрный" } }
        };

        // ── happy: первый SKU переводит товар в ON_MODERATION ──
        [Fact(DisplayName = "first_sku_transitions_product_to_on_moderation")]
        public async Task first_sku_transitions_product_to_on_moderation()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            var resp = await client.PostAsJsonAsync("/api/v1/skus", ValidSkuBody(productId));
            resp.StatusCode.Should().Be(HttpStatusCode.Created);

            // Проверяем статус товара в БД
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var product = await db.Products.AsNoTracking()
                .FirstAsync(p => p.Id == productId);

            product.Status.Should().Be(ProductStatus.OnModeration);
        }

        // ── happy: первый SKU порождает событие CREATED (sent_to_moderation) в Outbox ──
        [Fact(DisplayName = "first_sku_emits_created_event_to_moderation")]
        public async Task first_sku_emits_created_event_to_moderation()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            await client.PostAsJsonAsync("/api/v1/skus", ValidSkuBody(productId));

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();

            // Событие ухода на модерацию записано в Outbox с нужным типом и aggregate_id
            var evt = await db.OutboxMessages.AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.EventType == "product.sent_to_moderation.v1"
                    && m.AggregateId == productId);

            evt.Should().NotBeNull("первый SKU должен породить событие в Moderation");
        }

        // ── happy: второй SKU не меняет статус и не шлёт событие ──
        [Fact(DisplayName = "second_sku_no_state_change")]
        public async Task second_sku_no_state_change()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            // первый SKU
            await client.PostAsJsonAsync("/api/v1/skus", ValidSkuBody(productId));

            // считаем события до второго SKU
            int eventsBefore;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                eventsBefore = await db.OutboxMessages
                    .CountAsync(m => m.AggregateId == productId
                        && m.EventType == "product.sent_to_moderation.v1");
            }

            // второй SKU
            var second = ValidSkuBody(productId);
            var resp = await client.PostAsJsonAsync("/api/v1/skus", second);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);

            using var scope2 = _factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<B2BDbContext>();

            // статус остался ON_MODERATION (не изменился)
            var product = await db2.Products.AsNoTracking().FirstAsync(p => p.Id == productId);
            product.Status.Should().Be(ProductStatus.OnModeration);

            // новых sent_to_moderation событий не добавилось
            var eventsAfter = await db2.OutboxMessages
                .CountAsync(m => m.AggregateId == productId
                    && m.EventType == "product.sent_to_moderation.v1");
            eventsAfter.Should().Be(eventsBefore, "второй SKU не должен слать событие на модерацию");
        }

        // ── unhappy: добавление SKU к HARD_BLOCKED товару → 403 ──
        [Fact(DisplayName = "add_sku_to_hard_blocked_returns_403")]
        public async Task add_sku_to_hard_blocked_returns_403()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            // Переводим товар в HARD_BLOCKED напрямую в БД (модерация — отдельный flow)
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var product = await db.Products.FirstAsync(p => p.Id == productId);
                // HardBlock доступен из любого статуса; reports/skuIds пустые
                product.HardBlock(
                    new BlockingReason(Guid.NewGuid(), "Нарушение правил", "x"),
                     new List<(FieldReportTarget, Guid?, string)>(),
                     new List<Guid>(),
                     DateTime.UtcNow);
                await db.SaveChangesAsync();
            }

            var resp = await client.PostAsJsonAsync("/api/v1/skus", ValidSkuBody(productId));
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        [Fact(DisplayName = "add_sku_to_moderated_product_triggers_remoderation")]
        public async Task add_sku_to_moderated_product_triggers_remoderation()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            // Доводим товар до MODERATED: добавили первый SKU (→ ON_MODERATION) + Approve()
            await client.PostAsJsonAsync("/api/v1/skus", ValidSkuBody(productId));
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var product = await db.Products.Include(p => p.FieldReports).FirstAsync(p => p.Id == productId);
                product.Approve();
                await db.SaveChangesAsync();
            }

            // Считаем события sent_to_moderation ДО добавления второго SKU
            int eventsBefore;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                eventsBefore = await db.OutboxMessages.CountAsync(m =>
                    m.AggregateId == productId && m.EventType == "product.sent_to_moderation.v1");
            }

            // Добавляем ещё один SKU к MODERATED-товару
            var secondSku = new
            {
                product_id = productId,
                name = "Second",
                price = 1000000,
                discount = 0,
                cost_price = 500000,
                article = "ART-2",
                images = new[] { new { url = "/s3/s2.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var resp = await client.PostAsJsonAsync("/api/v1/skus", secondSku);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);

            using var scope2 = _factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<B2BDbContext>();

            // Статус вернулся на ON_MODERATION
            var productAfter = await db2.Products.AsNoTracking().FirstAsync(p => p.Id == productId);
            productAfter.Status.Should().Be(ProductStatus.OnModeration,
                "изменение состава SKU у MODERATED-товара триггерит повторную модерацию");

            // Появилось новое событие sent_to_moderation
            var eventsAfter = await db2.OutboxMessages.CountAsync(m =>
                m.AggregateId == productId && m.EventType == "product.sent_to_moderation.v1");
            eventsAfter.Should().Be(eventsBefore + 1, "должно появиться новое событие отправки на модерацию");
        }

        // ── добавление SKU к BLOCKED-товару тоже триггерит повторную модерацию ──
        [Fact(DisplayName = "add_sku_to_blocked_product_triggers_remoderation")]
        public async Task add_sku_to_blocked_product_triggers_remoderation()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            // CREATED → (первый SKU) → ON_MODERATION → Block() → BLOCKED
            await client.PostAsJsonAsync("/api/v1/skus", ValidSkuBody(productId));
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var product = await db.Products.Include(p => p.FieldReports).FirstAsync(p => p.Id == productId);
                product.Block(
                    new BlockingReason(Guid.NewGuid(), "Нарушение правил", "x"),
                    new List<(FieldReportTarget, Guid?, string)>(),
                    new List<Guid>(),
                    DateTime.UtcNow);
                await db.SaveChangesAsync();
            }

            int eventsBefore;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                eventsBefore = await db.OutboxMessages.CountAsync(m =>
                    m.AggregateId == productId && m.EventType == "product.sent_to_moderation.v1");
            }

            // Добавляем второй SKU к BLOCKED-товару
            var second = new
            {
                product_id = productId,
                name = "Second",
                price = 1000000,
                discount = 0,
                cost_price = 500000,
                article = "ART-3",
                images = new[] { new { url = "/s3/s3.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var resp = await client.PostAsJsonAsync("/api/v1/skus", second);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);

            using var scope2 = _factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<B2BDbContext>();

            var productAfter = await db2.Products.AsNoTracking().FirstAsync(p => p.Id == productId);
            productAfter.Status.Should().Be(ProductStatus.OnModeration);

            var eventsAfter = await db2.OutboxMessages.CountAsync(m =>
                m.AggregateId == productId && m.EventType == "product.sent_to_moderation.v1");
            eventsAfter.Should().Be(eventsBefore + 1);
        }
    }
}
