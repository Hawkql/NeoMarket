using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
using FluentAssertions;
using Xunit;

namespace B2C.Api.Tests.Catalog
{
    [Collection("Sequential")]
    public sealed class ListProductsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ListProductsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
        }

        /// <summary>
        /// US-CAT-01: каталог отдаёт товары с обязательными полями
        /// (карточка), без cost_price/reserved_quantity (ACL).
        /// </summary>
        [Fact(DisplayName = "list_products_returns_card_without_internal_fields")]
        public async Task list_products_returns_acl_filtered_cards()
        {
            var productId = Guid.NewGuid();
            _factory.CatalogFake.SeedProductInList(new ProductSummary(
                productId, "Phone", "/img/phone.jpg",
                Price: 100_00, OldPrice: 120_00, Discount: 16,
                InStock: true, Rating: 4.5, ReviewsCount: 42));

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/products");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> LIST: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = JsonDocument.Parse(body).RootElement.GetProperty("items");
            items.GetArrayLength().Should().Be(1);

            var card = items[0];
            card.GetProperty("title").GetString().Should().Be("Phone");
            card.GetProperty("price").GetInt32().Should().Be(100_00);
            card.GetProperty("in_stock").GetBoolean().Should().BeTrue();

            // ACL: внутренних полей нет.
            card.TryGetProperty("cost_price", out _).Should().BeFalse(
                "cost_price НЕ должен попадать в API");
            card.TryGetProperty("reserved_quantity", out _).Should().BeFalse(
                "reserved_quantity НЕ должен попадать в API");
        }

        /// <summary>US-CAT-01: пагинация limit/offset.</summary>
        [Fact(DisplayName = "list_products_supports_pagination")]
        public async Task pagination_works()
        {
            for (var i = 0; i < 5; i++)
            {
                _factory.CatalogFake.SeedProductInList(new ProductSummary(
                    Guid.NewGuid(), $"Item-{i}", null,
                    Price: 1000 + i * 100, OldPrice: null, Discount: null,
                    InStock: true, Rating: null, ReviewsCount: null));
            }

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/products?limit=2&offset=2");
            var root = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            root.GetProperty("items").GetArrayLength().Should().Be(2);
            root.GetProperty("total_count").GetInt32().Should().Be(5);
        }

        /// <summary>US-CAT-01: сортировка price_asc.</summary>
        [Fact(DisplayName = "list_products_sort_price_asc")]
        public async Task sort_price_asc_returns_ascending_prices()
        {
            _factory.CatalogFake.SeedProductInList(new ProductSummary(
                Guid.NewGuid(), "C", null, Price: 300_00, OldPrice: null, Discount: null,
                InStock: true, Rating: null, ReviewsCount: null));
            _factory.CatalogFake.SeedProductInList(new ProductSummary(
                Guid.NewGuid(), "A", null, Price: 100_00, OldPrice: null, Discount: null,
                InStock: true, Rating: null, ReviewsCount: null));
            _factory.CatalogFake.SeedProductInList(new ProductSummary(
                Guid.NewGuid(), "B", null, Price: 200_00, OldPrice: null, Discount: null,
                InStock: true, Rating: null, ReviewsCount: null));

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/products?sort=price_asc");
            var items = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                .RootElement.GetProperty("items");

            items[0].GetProperty("price").GetInt32().Should().Be(100_00);
            items[1].GetProperty("price").GetInt32().Should().Be(200_00);
            items[2].GetProperty("price").GetInt32().Should().Be(300_00);
        }

        /// <summary>US-CAT-01: пустой результат — 200, не 404.</summary>
        [Fact(DisplayName = "list_products_empty_returns_200_with_empty_items")]
        public async Task empty_catalog_returns_200_with_zero_items()
        {
            // Каталог пуст.
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/products");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                .RootElement.GetProperty("items");
            items.GetArrayLength().Should().Be(0);
        }

        /// <summary>
        /// US-CAT-02: текстовый поиск возвращает товары, у которых title содержит запрос.
        /// </summary>
        [Fact(DisplayName = "search_matches_title")]
        public async Task search_finds_products_by_title()
        {
            _factory.CatalogFake.SeedProductInList(new ProductSummary(
                Guid.NewGuid(), "iPhone 15 Pro", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedProductInList(new ProductSummary(
                Guid.NewGuid(), "Samsung Galaxy", null, 80_00, null, null, true, null, null));

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/products?search=iPhone");
            var items = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                .RootElement.GetProperty("items");

            items.GetArrayLength().Should().Be(1);
            items[0].GetProperty("title").GetString().Should().Contain("iPhone");
        }

        /// <summary>
        /// US-CAT-02: поиск без результатов — пустой items, не ошибка.
        /// </summary>
        [Fact(DisplayName = "search_no_match_returns_empty")]
        public async Task search_without_matches_returns_empty_items()
        {
            _factory.CatalogFake.SeedProductInList(new ProductSummary(
                Guid.NewGuid(), "Phone", null, 100_00, null, null, true, null, null));

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/products?search=ZZZZ_nonexistent");
            var items = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                .RootElement.GetProperty("items");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            items.GetArrayLength().Should().Be(0);
        }
        /// <summary>US-CAT-02 acceptance: search меньше 3 символов → 400.</summary>
        [Fact(DisplayName = "search_too_short_returns_400")]
        public async Task search_less_than_3_chars_returns_400()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/products?search=ab");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> SEARCH SHORT: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}