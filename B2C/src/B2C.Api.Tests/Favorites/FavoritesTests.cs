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
            // FK: favorites.buyer_id → buyers.id. Перед запросом seed-им Buyer
            // с тем же id, что в JWT, иначе INSERT упадёт constraint violation.
            _factory.EnsureBuyer(buyerId);

            var client = _factory.CreateClient();
            var token = JwtTokenHelper.GenerateBuyerToken(buyerId);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        /// <summary>
        /// US-CART-01: добавление в избранное идемпотентно — повтор не создаёт дубль.
        /// </summary>
        [Fact(DisplayName = "add_favorite_is_idempotent")]
        public async Task adding_same_product_twice_does_not_duplicate()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var productId = Guid.NewGuid();
            SeedProduct(productId);

            var add1 = await client.PostAsync($"/api/v1/favorites/{productId}", content: null);
            add1.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var add2 = await client.PostAsync($"/api/v1/favorites/{productId}", content: null);
            add2.StatusCode.Should().Be(HttpStatusCode.NoContent,
                "повторное добавление должно быть идемпотентно");

            var listResp = await client.GetAsync("/api/v1/favorites");
            var body = await listResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> LIST: {body}");

            var array = JsonDocument.Parse(body).RootElement;
            array.GetArrayLength().Should().Be(1, "не должно быть дубля в избранном");
            array[0].GetProperty("product_id").GetGuid().Should().Be(productId);
        }

        /// <summary>
        /// US-CART-01: удаление из избранного идемпотентно.
        /// Повторный DELETE — тоже 204, даже если товара уже нет.
        /// </summary>
        [Fact(DisplayName = "remove_favorite_is_idempotent")]
        public async Task removing_non_existent_favorite_returns_204()
        {
            var buyerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(buyerId);

            var productId = Guid.NewGuid();
            SeedProduct(productId);

            // Никогда не добавляли, сразу удаляем — должно быть 204.
            var resp = await client.DeleteAsync($"/api/v1/favorites/{productId}");
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent,
                "удаление несуществующего — идемпотентно");
        }

        /// <summary>
        /// US-CART-01: GET /favorites обогащает данные через B2B batch-запрос.
        /// Удалённые в B2B товары просто не попадают в ответ (не ошибка).
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

            // Добавляем оба в избранное.
            (await client.PostAsync($"/api/v1/favorites/{existingProductId}", null)).EnsureSuccessStatusCode();
            (await client.PostAsync($"/api/v1/favorites/{ghostProductId}", null)).EnsureSuccessStatusCode();

            // Имитируем удаление одного из B2B.
            _factory.CatalogFake.RemoveProduct(ghostProductId);

            var listResp = await client.GetAsync("/api/v1/favorites");
            var body = await listResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> LIST AFTER REMOVE: {body}");

            var array = JsonDocument.Parse(body).RootElement;
            array.GetArrayLength().Should().Be(1,
                "удалённый в B2B товар не попадает в ответ");
            array[0].GetProperty("product_id").GetGuid().Should().Be(existingProductId);
        }

        /// <summary>
        /// US-CART-01: anonymous → 401 (избранное привязано к покупателю).
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