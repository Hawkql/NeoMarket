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

namespace B2C.Api.Tests.Auth
{
    [Collection("Sequential")]
    public sealed class GuestCartMergedOnLoginTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public GuestCartMergedOnLoginTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
            _factory.ReservationFake.Reset();
        }

        /// <summary>
        /// US-CART-03 ключевой acceptance:
        ///   1. Гость собирает корзину (X-Session-Id).
        ///   2. Регистрируется + логинится с тем же session_id.
        ///   3. После логина GET /cart с JWT возвращает позиции из гостевой корзины.
        /// </summary>
        [Fact(DisplayName = "guest_cart_merged_on_login")]
        public async Task guest_cart_is_merged_into_user_cart_on_login()
        {
            // Arrange.
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            SeedCatalog(productId, skuId, price: 100_00);

            const string email = "buyer@test.local";
            const string password = "test1234";
            const string sessionId = "guest-session-merge-1";

            // 1. Гость кладёт 2 шт в корзину.
            var guestClient = _factory.CreateClient();
            guestClient.DefaultRequestHeaders.Add("X-Session-Id", sessionId);

            var addResp = await guestClient.PostAsJsonAsync(
                "/api/v1/cart/items", new { sku_id = skuId, quantity = 2 });
            addResp.StatusCode.Should().Be(HttpStatusCode.NoContent,
                await addResp.Content.ReadAsStringAsync());

            // 2. Регистрация + login с тем же session_id.
            var registerClient = _factory.CreateClient();
            registerClient.DefaultRequestHeaders.Add("X-Session-Id", sessionId);

            var regResp = await registerClient.PostAsJsonAsync("/api/v1/auth/register",
                new { email, password, first_name = (string?)null, last_name = (string?)null, phone = (string?)null });
            regResp.StatusCode.Should().Be(HttpStatusCode.OK, await regResp.Content.ReadAsStringAsync());

            var loginClient = _factory.CreateClient();
            loginClient.DefaultRequestHeaders.Add("X-Session-Id", sessionId);

            var loginResp = await loginClient.PostAsJsonAsync("/api/v1/auth/login",
                new { email, password });
            loginResp.StatusCode.Should().Be(HttpStatusCode.OK, await loginResp.Content.ReadAsStringAsync());

            var loginBody = await loginResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> LOGIN: {loginBody}");
            var accessToken = JsonDocument.Parse(loginBody)
                .RootElement.GetProperty("access_token").GetString();

            // 3. GET /cart c JWT — должен видеть позицию из гостевой корзины.
            var authClient = _factory.CreateClient();
            authClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var cartResp = await authClient.GetAsync("/api/v1/cart");
            var cartBody = await cartResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> GET CART: {cartResp.StatusCode} BODY: {cartBody}");

            cartResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var root = JsonDocument.Parse(cartBody).RootElement;
            var items = root.GetProperty("items");
            items.GetArrayLength().Should().Be(1, "позиция из гостевой корзины должна перенестись");
            items[0].GetProperty("sku_id").GetGuid().Should().Be(skuId);
            items[0].GetProperty("quantity").GetInt32().Should().Be(2);
        }

        private void SeedCatalog(Guid productId, Guid skuId, int price)
        {
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Phone", null, price, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", price, Discount: 0, ImageUrl: null,
                InStock: true, Characteristics: Array.Empty<CharacteristicValue>()));
        }
    }
}