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
    public sealed class GetCartEnrichmentTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public GetCartEnrichmentTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
            _factory.ReservationFake.Reset();
        }

        /// <summary>
        /// US-CART-03: GET /cart показывает АКТУАЛЬНЫЕ цены из B2B.
        /// Меняем цену в каталоге → следующий GET возвращает новую цену
        /// (корзина НЕ замораживает цены — заморозка только при checkout).
        /// </summary>
        [Fact(DisplayName = "cart_shows_current_prices_from_b2b")]
        public async Task get_cart_returns_actual_prices_from_b2b()
        {
            var productId = Guid.NewGuid();
            var skuId = Guid.NewGuid();
            SeedCatalog(productId, skuId, price: 100_00);

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Session-Id", "guest-prices-1");

            await client.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuId, quantity = 1 });

            // Первая проверка — цена 100_00.
            var resp1 = await client.GetAsync("/api/v1/cart");
            var body1 = await resp1.Content.ReadAsStringAsync();
            var price1 = JsonDocument.Parse(body1).RootElement
                .GetProperty("items")[0].GetProperty("unit_price").GetInt32();
            price1.Should().Be(100_00);

            // Меняем цену в B2B-каталоге.
            SeedCatalog(productId, skuId, price: 150_00);

            // Второй GET — цена обновилась.
            var resp2 = await client.GetAsync("/api/v1/cart");
            var body2 = await resp2.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CART AFTER PRICE CHANGE: {body2}");

            var price2 = JsonDocument.Parse(body2).RootElement
                .GetProperty("items")[0].GetProperty("unit_price").GetInt32();
            price2.Should().Be(150_00, "цена в корзине должна быть актуальной из B2B");
        }

        /// <summary>
        /// US-CART-03: недоступные позиции (SKU out of stock в B2B) показываются
        /// с unavailable_reason и НЕ участвуют в total_amount.
        /// </summary>
        [Fact(DisplayName = "unavailable_items_not_in_total")]
        public async Task unavailable_items_remain_in_cart_but_not_in_total()
        {
            // Два товара. Один станет недоступен.
            var productAvailableId = Guid.NewGuid();
            var skuAvailableId = Guid.NewGuid();
            SeedCatalog(productAvailableId, skuAvailableId, price: 100_00);

            var productOutId = Guid.NewGuid();
            var skuOutId = Guid.NewGuid();
            SeedCatalog(productOutId, skuOutId, price: 200_00);

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Session-Id", "guest-unavail-1");

            await client.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuAvailableId, quantity = 1 });
            await client.PostAsJsonAsync("/api/v1/cart/items",
                new { sku_id = skuOutId, quantity = 1 });

            // Один из SKU стал out_of_stock.
            _factory.CatalogFake.MarkOutOfStock(skuOutId);

            var resp = await client.GetAsync("/api/v1/cart");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CART WITH UNAVAILABLE: {body}");

            var root = JsonDocument.Parse(body).RootElement;
            var items = root.GetProperty("items");
            items.GetArrayLength().Should().Be(2, "обе позиции остаются в корзине");

            // total_amount = только доступная (100_00), без out-of-stock.
            root.GetProperty("total_amount").GetInt32().Should().Be(100_00,
                "недоступные позиции не учитываются в total");

            root.GetProperty("available_items_count").GetInt32().Should().Be(1);
            root.GetProperty("unavailable_items_count").GetInt32().Should().Be(1);
        }

        /// <summary>
        /// US-CART-03: GET /cart без позиций возвращает пустой CartDto (не 404).
        /// </summary>
        [Fact(DisplayName = "empty_cart_returns_200_with_empty_items")]
        public async Task empty_cart_returns_200_with_zero_items()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Session-Id", "guest-empty-1");

            var resp = await client.GetAsync("/api/v1/cart");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var root = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            root.GetProperty("items").GetArrayLength().Should().Be(0);
            root.GetProperty("total_amount").GetInt32().Should().Be(0);
        }

        private void SeedCatalog(Guid productId, Guid skuId, int price)
        {
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                productId, "Product", null, price, null, null, true, null, null));
            _factory.CatalogFake.SeedSku(new SkuInfo(
                skuId, productId, "Default", price, Discount: 0, ImageUrl: null,
                InStock: true, Characteristics: Array.Empty<CharacteristicValue>()));
        }
    }
}