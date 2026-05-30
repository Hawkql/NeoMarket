using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;

using FluentAssertions;
using Xunit;

namespace B2C.Api.Tests.Cart
{
    [Collection("Sequential")]
    public sealed class AddToCartTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AddToCartTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
            _factory.ReservationFake.Reset();
        }

        /// <summary>
        /// US-CART-03: повторное добавление того же SKU увеличивает quantity, не создаёт дубль.
        /// Проверяем через гостевой сценарий (X-Session-Id) — auth-вариант работает идентично.
        /// </summary>
        [Fact(DisplayName = "add_same_sku_increases_quantity_no_duplicate")]
        public async Task adding_same_sku_twice_increases_quantity()
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Phone", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", 100_00, Discount: 0, ImageUrl: null,
                InStock: true, Characteristics: Array.Empty<CharacteristicValue>()));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Session-Id", "guest-add-twice-1");

            // Первое добавление — 2 шт.
            var add1 = await client.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuId, quantity = 2 });
            add1.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Второе добавление того же SKU — 3 шт.
            var add2 = await client.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuId, quantity = 3 });
            add2.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // GET /cart — одна позиция с quantity=5.
            var cartResp = await client.GetAsync("/api/v1/cart");
            var body = await cartResp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CART: {body}");

            var items = JsonDocument.Parse(body).RootElement.GetProperty("items");
            items.GetArrayLength().Should().Be(1, "не должно быть двух одинаковых позиций");
            items[0].GetProperty("quantity").GetInt32().Should().Be(5, "2 + 3 = 5");
        }

        /// <summary>
        /// US-CART-03: без JWT и без X-Session-Id → 401.
        /// ICartContextResolver требует хоть какой-то идентификатор.
        /// </summary>
        [Fact(DisplayName = "anonymous_without_session_returns_401")]
        public async Task add_to_cart_without_session_or_jwt_returns_401()
        {
            var client = _factory.CreateClient();
            // НИ X-Session-Id, НИ Authorization.

            var resp = await client.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = Guid.NewGuid(), quantity = 1 });

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Несуществующий SKU → 404.
        /// </summary>
        [Fact(DisplayName = "add_unknown_sku_returns_404")]
        public async Task add_unknown_sku_returns_404()
        {
            // Каталог пуст.
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Session-Id", "guest-unknown-sku-1");

            var resp = await client.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = Guid.NewGuid(), quantity = 1 });
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> ADD UNKNOWN: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        /// <summary>
        /// US-CART-03 best-effort: добавление out-of-stock SKU → 400.
        /// (Точная проверка active_quantity делегирована reserve при checkout —
        /// в API B2C нет числа остатков, только bool InStock.)
        /// </summary>
        [Fact(DisplayName = "add_out_of_stock_sku_returns_400")]
        public async Task adding_sku_with_in_stock_false_returns_400()
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Phone", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", 100_00, Discount: 0, ImageUrl: null,
                InStock: false,    // ← out of stock
                Characteristics: Array.Empty<CharacteristicValue>()));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Session-Id", "guest-out-of-stock-1");

            var resp = await client.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuId, quantity = 1 });
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> OUT_OF_STOCK: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}