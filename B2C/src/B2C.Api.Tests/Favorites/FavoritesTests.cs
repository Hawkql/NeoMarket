using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
using FluentAssertions;
using Xunit;

namespace B2C.Api.Tests.Favorites
{
    [Collection("Sequential")]
    public sealed class FavoritesTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public FavoritesTests(CustomWebApplicationFactory factory)
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
        /// US-CART-01: openapi PUT /api/v1/favorites/{product_id} — 204 идемпотентно.
        /// </summary>
        [Fact(DisplayName = "add_favorite_is_idempotent")]
        public async Task adding_same_product_twice_does_not_duplicate()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var productId = Guid.NewGuid();
            SeedProduct(productId);

            var add1 = await client.PutAsync($"/api/v1/favorites/{productId}", content: null);
            add1.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var add2 = await client.PutAsync($"/api/v1/favorites/{productId}", content: null);
            add2.StatusCode.Should().Be(HttpStatusCode.NoContent,
                "повторное добавление должно быть идемпотентно");

            var listResp = await client.GetAsync("/api/v1/favorites");
            var body = await listResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> LIST: {body}");

            var items = JsonDocument.Parse(body).RootElement.GetProperty("items");
            items.GetArrayLength().Should().Be(1, "не должно быть дубля в избранном");
            items[0].GetProperty("id").GetGuid().Should().Be(productId);
        }

        /// <summary>
        /// US-CART-01: удаление из избранного идемпотентно.
        /// </summary>
        [Fact(DisplayName = "remove_favorite_is_idempotent")]
        public async Task removing_non_existent_favorite_returns_204()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var productId = Guid.NewGuid();
            SeedProduct(productId);

            var resp = await client.DeleteAsync($"/api/v1/favorites/{productId}");
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent,
                "удаление несуществующего — идемпотентно");
        }

        /// <summary>
        /// US-CART-01: GET /favorites возвращает PaginatedCatalogProducts.
        /// Удалённые в B2B товары — silent skip.
        /// </summary>
        [Fact(DisplayName = "deleted_in_b2b_products_excluded_from_favorites")]
        public async Task favorites_excludes_products_removed_from_b2b_catalog()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var existingProductId = Guid.NewGuid();
            var ghostProductId = Guid.NewGuid();

            SeedProduct(existingProductId, "Existing");
            SeedProduct(ghostProductId, "Will-be-removed");

            (await client.PutAsync($"/api/v1/favorites/{existingProductId}", null)).EnsureSuccessStatusCode();
            (await client.PutAsync($"/api/v1/favorites/{ghostProductId}", null)).EnsureSuccessStatusCode();

            _factory.CatalogFake.RemoveProduct(ghostProductId);

            var listResp = await client.GetAsync("/api/v1/favorites");
            var body = await listResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> LIST AFTER REMOVE: {body}");

            var root = JsonDocument.Parse(body).RootElement;
            var items = root.GetProperty("items");
            items.GetArrayLength().Should().Be(1,
                "удалённый в B2B товар не попадает в ответ");
            items[0].GetProperty("id").GetGuid().Should().Be(existingProductId);

            // openapi PaginatedCatalogProducts требует total_count/limit/offset.
            root.GetProperty("total_count").GetInt32().Should().Be(2,
                "total_count считает все Favorite, даже удалённые в B2B");
        }

        /// <summary>
        /// US-CART-01: anonymous → 401.
        /// </summary>
        [Fact(DisplayName = "favorites_require_authorization")]
        public async Task favorites_without_jwt_return_401()
        {
            var client = _factory.CreateClient();

            var resp = await client.GetAsync("/api/v1/favorites");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private void SeedProduct(Guid productId, string title = "Test Product")
        {
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, title, null, 100_00, null, null, true, null, null));
        }
    }
}