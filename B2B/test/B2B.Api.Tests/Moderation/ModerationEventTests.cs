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

namespace B2B.Api.Tests.Moderation
{
    [Collection("Sequential")]
    public sealed class ModerationEventTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string ServiceKey = "test-service-key";

        // Произвольный uuid причины. Контракт OpenAPI требует только тип uuid,
        // содержимое не валидируется на стороне B2B (это межсервисный id из Moderation).
        private static readonly Guid SampleReasonId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        private readonly CustomWebApplicationFactory _factory;

        public ModerationEventTests(CustomWebApplicationFactory factory) => _factory = factory;

        private HttpClient SellerClient(Guid sellerId)
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return c;
        }

        private HttpClient ServiceClient()
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Add("X-Service-Key", ServiceKey);
            return c;
        }

        // Товар с SKU → OnModeration (готов к решению модерации)
        private async Task<(Guid productId, Guid sellerId)> ProductOnModerationAsync()
        {
            var sellerId = Guid.NewGuid();
            var seller = SellerClient(sellerId);
            var prod = new
            {
                category_id = TestData.CategoryId,
                title = "Mod P",
                description = "d",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var pid = JsonDocument.Parse(await (await seller.PostAsJsonAsync("/api/v1/products", prod))
                .Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
            var sku = new
            {
                product_id = pid,
                name = "S",
                price = 1000000,
                discount = 0,
                cost_price = 500000,
                article = "A",
                images = new[] { new { url = "/s3/s.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            await seller.PostAsJsonAsync("/api/v1/skus", sku);   // → OnModeration
            return (pid, sellerId);
        }

        private async Task<ProductStatus> GetStatusAsync(Guid productId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            return (await db.Products.AsNoTracking().FirstAsync(p => p.Id == productId)).Status;
        }

        // ── MODERATED очищает blocking-данные ──
        [Fact(DisplayName = "moderated_event_clears_blocking_data")]
        public async Task moderated_event_clears_blocking_data()
        {
            var (pid, _) = await ProductOnModerationAsync();

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                product_id = pid,
                event_type = "MODERATED",
                hard_block = false,
                occurred_at = DateTime.UtcNow
            };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/moderation/events", body);
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await GetStatusAsync(pid)).Should().Be(ProductStatus.Moderated);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var product = await db.Products.AsNoTracking().FirstAsync(p => p.Id == pid);
            product.BlockingReason.Should().BeNull("MODERATED очищает причину блокировки");
        }

        // ── BLOCKED (soft) сохраняет field_reports ──
        [Fact(DisplayName = "blocked_soft_saves_field_reports")]
        public async Task blocked_soft_saves_field_reports()
        {
            var (pid, _) = await ProductOnModerationAsync();

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                product_id = pid,
                event_type = "BLOCKED",
                hard_block = false,
                blocking_reason_id = SampleReasonId,
                moderator_comment = "коммент модератора",
                field_reports = new[]
                {
                    new { field_name = "description", sku_id = (Guid?)null, comment = "плохое описание" }
                },
                occurred_at = DateTime.UtcNow
            };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/moderation/events", body);
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await GetStatusAsync(pid)).Should().Be(ProductStatus.Blocked);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var product = await db.Products.Include(p => p.FieldReports).AsNoTracking()
                .FirstAsync(p => p.Id == pid);
            product.FieldReports.Should().NotBeEmpty("BLOCKED сохраняет замечания по полям");
        }

        // ── BLOCKED hard → HARD_BLOCKED ──
        [Fact(DisplayName = "blocked_hard_sets_terminal_status")]
        public async Task blocked_hard_sets_terminal_status()
        {
            var (pid, _) = await ProductOnModerationAsync();

            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                product_id = pid,
                event_type = "BLOCKED",
                hard_block = true,
                blocking_reason_id = SampleReasonId,
                moderator_comment = "грубое нарушение",
                occurred_at = DateTime.UtcNow
            };
            var resp = await ServiceClient().PostAsJsonAsync("/api/v1/moderation/events", body);
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await GetStatusAsync(pid)).Should().Be(ProductStatus.HardBlocked);
        }

        // ── после HARD_BLOCKED правки продавца → 403 ──
        [Fact(DisplayName = "hard_blocked_product_rejects_seller_edits")]
        public async Task hard_blocked_product_rejects_seller_edits()
        {
            var (pid, sellerId) = await ProductOnModerationAsync();

            var block = new
            {
                idempotency_key = Guid.NewGuid(),
                product_id = pid,
                event_type = "BLOCKED",
                hard_block = true,
                blocking_reason_id = SampleReasonId,
                moderator_comment = "x",
                occurred_at = DateTime.UtcNow
            };
            (await ServiceClient().PostAsJsonAsync("/api/v1/moderation/events", block))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Продавец пытается редактировать HARD_BLOCKED
            var edit = new { title = "try", description = "try" };
            var resp = await SellerClient(sellerId).PutAsJsonAsync($"/api/v1/products/{pid}", edit);
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        // ── дубль события с тем же ключом → без эффекта ──
        [Fact(DisplayName = "duplicate_event_same_idempotency_key_no_side_effects")]
        public async Task duplicate_event_same_idempotency_key_no_side_effects()
        {
            var (pid, _) = await ProductOnModerationAsync();
            var key = Guid.NewGuid();
            var body = new
            {
                idempotency_key = key,
                product_id = pid,
                event_type = "MODERATED",
                hard_block = false,
                occurred_at = DateTime.UtcNow
            };

            (await ServiceClient().PostAsJsonAsync("/api/v1/moderation/events", body))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await GetStatusAsync(pid)).Should().Be(ProductStatus.Moderated);

            // Повтор тем же ключом — Approve второй раз кинул бы (Moderated→Approve запрещён),
            // но идемпотентность вернёт раньше → без эффекта и без ошибки.
            var dup = await ServiceClient().PostAsJsonAsync("/api/v1/moderation/events", body);
            dup.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await GetStatusAsync(pid)).Should().Be(ProductStatus.Moderated);
        }

        // ── без X-Service-Key → 401 ──
        [Fact(DisplayName = "missing_service_key_returns_401")]
        public async Task missing_service_key_returns_401()
        {
            var (pid, _) = await ProductOnModerationAsync();
            var noKey = _factory.CreateClient();
            var body = new
            {
                idempotency_key = Guid.NewGuid(),
                product_id = pid,
                event_type = "MODERATED",
                hard_block = false,
                occurred_at = DateTime.UtcNow
            };
            var resp = await noKey.PostAsJsonAsync("/api/v1/moderation/events", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Опциональная страховка: тело плоское {code, message} (после US-07 fix)
            var json = await resp.Content.ReadAsStringAsync();
            JsonDocument.Parse(json).RootElement.GetProperty("code").GetString()
                .Should().Be("UNAUTHORIZED");
        }
    }
}