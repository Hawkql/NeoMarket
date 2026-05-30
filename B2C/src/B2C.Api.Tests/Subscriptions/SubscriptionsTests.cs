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

        /// <summary>
        /// US-CART-02: подписка сохраняется. notify_on — массив строк.
        /// </summary>
        [Fact(DisplayName = "subscribe_creates_subscription")]
        public async Task subscribe_to_product_persists_with_notify_on()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var productId = Guid.NewGuid();

            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Test", null, 100_00, null, null, true, null, null));

            var resp = await client.PostAsJsonAsync($"/api/v1/subscriptions/{productId}",
                new { notify_on = new[] { "in_stock", "price_drop" } });

            resp.StatusCode.Should().Be(HttpStatusCode.NoContent,
                await resp.Content.ReadAsStringAsync());


            var listResp = await client.GetAsync("/api/v1/subscriptions");
            var body = await listResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> LIST SUBS: {body}");

            var arr = JsonDocument.Parse(body).RootElement;
            arr.GetArrayLength().Should().Be(1);
            arr[0].GetProperty("product_id").GetGuid().Should().Be(productId);
        }

        /// <summary>
        /// US-CART-02: повторная подписка — upsert (NoContent, без 409 — мы сделали upsert,
        /// см. Application/Subscriptions/Commands/Subscribe).
        /// </summary>
        [Fact(DisplayName = "subscribe_twice_returns_409")]
        public async Task subscribe_twice_returns_409_conflict()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);
            var productId = Guid.NewGuid();

            // Товар должен существовать в B2B (иначе уже 404).
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Test", null, 100_00, null, null, true, null, null));

            var first = await client.PostAsJsonAsync($"/api/v1/subscriptions/{productId}",
                new { notify_on = new[] { "in_stock" } });
            first.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var second = await client.PostAsJsonAsync($"/api/v1/subscriptions/{productId}",
                new { notify_on = new[] { "price_drop" } });
            var body = await second.Content.ReadAsStringAsync();
            Console.WriteLine($">>> SECOND: {second.StatusCode} BODY: {body}");

            second.StatusCode.Should().Be(HttpStatusCode.Conflict);
            JsonDocument.Parse(body).RootElement.GetProperty("code").GetString()
                .Should().Be("ALREADY_SUBSCRIBED");
        }
        [Fact(DisplayName = "subscribe_unknown_product_returns_404")]
        public async Task subscribe_to_unknown_product_returns_404()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            // Каталог пуст — товара нет.
            var resp = await client.PostAsJsonAsync(
                $"/api/v1/subscriptions/{Guid.NewGuid()}",
                new { notify_on = new[] { "in_stock" } });
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> UNKNOWN: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
            JsonDocument.Parse(body).RootElement.GetProperty("code").GetString()
                .Should().Be("PRODUCT_NOT_FOUND");
        }

        /// <summary>
        /// US-CART-02: пустой notify_on → 400.
        /// </summary>
        [Fact(DisplayName = "subscribe_empty_notify_on_returns_400")]
        public async Task subscribe_with_empty_notify_on_returns_400()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var resp = await client.PostAsJsonAsync($"/api/v1/subscriptions/{Guid.NewGuid()}",
                new { notify_on = Array.Empty<string>() });

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// US-CART-02: невалидное значение → 400.
        /// </summary>
        [Fact(DisplayName = "subscribe_invalid_notify_on_returns_400")]
        public async Task subscribe_with_unknown_notify_on_returns_400()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var resp = await client.PostAsJsonAsync($"/api/v1/subscriptions/{Guid.NewGuid()}",
                new { notify_on = new[] { "unknown_event" } });

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// US-CART-02: отписка — идемпотентна, повтор 204.
        /// </summary>
        [Fact(DisplayName = "unsubscribe_is_idempotent")]
        public async Task unsubscribe_non_existent_returns_204()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var resp = await client.DeleteAsync($"/api/v1/subscriptions/{Guid.NewGuid()}");
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent,
                "delete несуществующей — идемпотентно");
        }

        /// <summary>
        /// US-CART-02: anonymous → 401.
        /// </summary>
        [Fact(DisplayName = "subscriptions_require_authorization")]
        public async Task subscriptions_without_jwt_return_401()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/subscriptions");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}