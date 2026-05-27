using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using B2B.Api.Tests.Infrastructure;
using B2B.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2B.Api.Tests.Products
{
    [Collection("Sequential")]
    public sealed class DeleteProductTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DeleteProductTests(CustomWebApplicationFactory factory) => _factory = factory;

        private HttpClient AuthClient(Guid sellerId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return client;
        }

        private async Task<Guid> CreateProductAsync(HttpClient client)
        {
            var body = new
            {
                category_id = TestData.CategoryId,
                title = "To Delete",
                description = "desc",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var resp = await client.PostAsJsonAsync("/api/v1/products", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
        }

        // ── deleted=true в БД ──
        [Fact(DisplayName = "delete_sets_deleted_true")]
        public async Task delete_sets_deleted_true()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            var resp = await client.DeleteAsync($"/api/v1/products/{productId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var product = await db.Products.AsNoTracking().FirstAsync(p => p.Id == productId);
            product.Deleted.Should().BeTrue();
        }

        // ── событие product.deleted.v1 для moderation ──
        [Fact(DisplayName = "delete_emits_event_to_moderation")]
        public async Task delete_emits_event_to_moderation()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            await client.DeleteAsync($"/api/v1/products/{productId}");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var evt = await db.OutboxMessages.AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.EventType == "product.deleted.v1"
                    && m.AggregateId == productId
                    && m.Destination == "moderation");
            evt.Should().NotBeNull("удаление шлёт product.deleted.v1 в moderation");
        }

        // ── событие product.deleted.v1 для b2c ──
        [Fact(DisplayName = "delete_emits_product_deleted_to_b2c")]
        public async Task delete_emits_product_deleted_to_b2c()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            await client.DeleteAsync($"/api/v1/products/{productId}");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var evt = await db.OutboxMessages.AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.EventType == "product.deleted.v1"
                    && m.AggregateId == productId
                    && m.Destination == "b2c");
            evt.Should().NotBeNull("удаление шлёт product.deleted.v1 в b2c");
        }

        // ── повторное удаление → 400 ──
        [Fact(DisplayName = "delete_already_deleted_returns_400")]
        public async Task delete_already_deleted_returns_400()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            var first = await client.DeleteAsync($"/api/v1/products/{productId}");
            first.StatusCode.Should().Be(HttpStatusCode.OK);

            var second = await client.DeleteAsync($"/api/v1/products/{productId}");
            second.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var json = await second.Content.ReadAsStringAsync();
            JsonDocument.Parse(json).RootElement.GetProperty("code").GetString()
                .Should().Be("INVALID_REQUEST");
        }
        [Fact(DisplayName = "delete_others_product_returns_403")]
        public async Task delete_others_product_returns_403()
        {
            // владелец создаёт товар
            var ownerId = Guid.NewGuid();
            var owner = AuthClient(ownerId);
            var createBody = new
            {
                category_id = TestData.CategoryId,
                title = "Owner product",
                description = "desc",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var created = await owner.PostAsJsonAsync("/api/v1/products", createBody);
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            var productId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
                .RootElement.GetProperty("id").GetGuid();

            // другой продавец пытается удалить
            var attacker = AuthClient(Guid.NewGuid());
            var resp = await attacker.DeleteAsync($"/api/v1/products/{productId}");

            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var json = await resp.Content.ReadAsStringAsync();
            JsonDocument.Parse(json).RootElement.GetProperty("code").GetString()
                .Should().Be("NOT_OWNER");
        }
        // ── удалённый не виден в списке продавца ──
        [Fact(DisplayName = "deleted_product_not_in_seller_list")]
        public async Task deleted_product_not_in_seller_list()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);

            await client.DeleteAsync($"/api/v1/products/{productId}");

            var listResp = await client.GetAsync("/api/v1/products");
            listResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await listResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("items");
            var ids = items.EnumerateArray()
                .Select(e => e.GetProperty("id").GetGuid())
                .ToList();
            ids.Should().NotContain(productId, "удалённый товар скрыт из списка по умолчанию");
        }
    }
}
