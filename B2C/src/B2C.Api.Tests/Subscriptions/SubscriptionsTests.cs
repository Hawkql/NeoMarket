using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
using FluentAssertions;
using Xunit;

namespace B2C.Api.Tests.Subscriptions
{
    [Collection("Sequential")]
    public sealed class SubscriptionsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public SubscriptionsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
        }

        private HttpClient CreateAuthorizedClient(Guid buyerId)
        {
            _factory.EnsureBuyer(buyerId);
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateBuyerToken(buyerId));
            return client;
        }

        private void SeedProduct(Guid productId) =>
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Test", null, 100_00, null, null, true, null, null));

        // ==================== POST/DELETE под /favorites/{id}/subscribe (openapi) ====================

        [Fact(DisplayName = "subscribe_creates_subscription")]
        public async Task subscribe_to_product_returns_204()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var productId = Guid.NewGuid();
            SeedProduct(productId);

            var resp = await client.PostAsJsonAsync(
                $"/api/v1/favorites/{productId}/subscribe",
                new { events = new[] { "BACK_IN_STOCK", "PRICE_DROP" } });

            resp.StatusCode.Should().Be(HttpStatusCode.NoContent,
                await resp.Content.ReadAsStringAsync());
        }

        [Fact(DisplayName = "subscribe_with_empty_body_uses_default_events")]
        public async Task subscribe_without_body_uses_default()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var productId = Guid.NewGuid();
            SeedProduct(productId);

            var resp = await client.PostAsJsonAsync(
                $"/api/v1/favorites/{productId}/subscribe", new { });

            resp.StatusCode.Should().Be(HttpStatusCode.NoContent,
                await resp.Content.ReadAsStringAsync());
        }

        [Fact(DisplayName = "subscribe_twice_returns_409")]
        public async Task subscribe_twice_returns_409_conflict()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var productId = Guid.NewGuid();
            SeedProduct(productId);

            var first = await client.PostAsJsonAsync(
                $"/api/v1/favorites/{productId}/subscribe",
                new { events = new[] { "BACK_IN_STOCK" } });
            first.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var second = await client.PostAsJsonAsync(
                $"/api/v1/favorites/{productId}/subscribe",
                new { events = new[] { "PRICE_DROP" } });
            var body = await second.Content.ReadAsStringAsync();

            second.StatusCode.Should().Be(HttpStatusCode.Conflict);
            JsonDocument.Parse(body).RootElement.GetProperty("code").GetString()
                .Should().Be("ALREADY_SUBSCRIBED");
        }

        [Fact(DisplayName = "subscribe_unknown_product_returns_404")]
        public async Task subscribe_to_unknown_product_returns_404()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var resp = await client.PostAsJsonAsync(
                $"/api/v1/favorites/{Guid.NewGuid()}/subscribe",
                new { events = new[] { "BACK_IN_STOCK" } });
            var body = await resp.Content.ReadAsStringAsync();

            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
            JsonDocument.Parse(body).RootElement.GetProperty("code").GetString()
                .Should().Be("PRODUCT_NOT_FOUND");
        }

        [Fact(DisplayName = "subscribe_invalid_event_returns_400")]
        public async Task subscribe_with_unknown_event_returns_400()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var productId = Guid.NewGuid();
            SeedProduct(productId);

            var resp = await client.PostAsJsonAsync(
                $"/api/v1/favorites/{productId}/subscribe",
                new { events = new[] { "UNKNOWN_EVENT" } });

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "unsubscribe_is_idempotent")]
        public async Task unsubscribe_non_existent_returns_204()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var resp = await client.DeleteAsync(
                $"/api/v1/favorites/{Guid.NewGuid()}/subscribe");
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // ==================== GET /api/v1/subscriptions (наше расширение) ====================

        /// <summary>
        /// Расширение поверх openapi: список подписок покупателя.
        /// Ревьюер засчитал этот endpoint в US-CART-02 — оставляем.
        /// </summary>
        [Fact(DisplayName = "list_subscriptions_returns_subscribed_products")]
        public async Task list_returns_subscribed_after_subscribe()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var productId = Guid.NewGuid();
            SeedProduct(productId);

            await client.PostAsJsonAsync(
                $"/api/v1/favorites/{productId}/subscribe",
                new { events = new[] { "BACK_IN_STOCK" } });

            var listResp = await client.GetAsync("/api/v1/subscriptions");
            var body = await listResp.Content.ReadAsStringAsync();

            listResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var arr = JsonDocument.Parse(body).RootElement;
            arr.GetArrayLength().Should().Be(1);
            arr[0].GetProperty("product_id").GetGuid().Should().Be(productId);
        }

        [Fact(DisplayName = "subscriptions_require_authorization")]
        public async Task get_subscriptions_without_jwt_returns_401()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/subscriptions");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}