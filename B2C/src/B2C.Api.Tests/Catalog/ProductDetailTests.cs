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
    public sealed class ProductDetailTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ProductDetailTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
        }

        /// <summary>
        /// US-CAT-03: карточка возвращает description, images, characteristics, список SKU.
        /// </summary>
        [Fact(DisplayName = "product_detail_returns_full_card")]
        public async Task get_product_returns_detail_with_skus()
        {
            var productId = Guid.NewGuid();
            var sku1Id = Guid.NewGuid();
            var sku2Id = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            _factory.CatalogFake.SeedProductDetail(new ProductDetail(
                Id: productId,
                Title: "iPhone 15 Pro",
                Description: "Flagship",
                CategoryId: categoryId,
                ImageUrls: new[] { "/img/1.jpg", "/img/2.jpg" },
                Skus: new[]
                {
                    new SkuInfo(sku1Id, productId, "Black 256GB", 100_00, Discount: 10, "/img/black.jpg",
                        InStock: true, Characteristics: Array.Empty<CharacteristicValue>()),
                    new SkuInfo(sku2Id, productId, "White 256GB", 100_00, Discount: 0, "/img/white.jpg",
                        InStock: false, Characteristics: Array.Empty<CharacteristicValue>()),
                },
                Characteristics: new[]
                {
                    new CharacteristicValue("Brand", "Apple"),
                    new CharacteristicValue("Year", "2025"),
                },
                Rating: 4.8,
                ReviewsCount: 153));

            var client = _factory.CreateClient();
            var resp = await client.GetAsync($"/api/v1/catalog/products/{productId}");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> DETAIL: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var root = JsonDocument.Parse(body).RootElement;

            root.GetProperty("title").GetString().Should().Be("iPhone 15 Pro");
            root.GetProperty("image_urls").GetArrayLength().Should().Be(2);
            root.GetProperty("characteristics").GetArrayLength().Should().Be(2);

            var skus = root.GetProperty("skus");
            skus.GetArrayLength().Should().Be(2);

            // SKU без остатка ОТОБРАЖАЕТСЯ (но фронт сделает кнопку неактивной по in_stock=false).
            var unavailableSku = skus[1];
            unavailableSku.GetProperty("in_stock").GetBoolean().Should().BeFalse();
        }

        /// <summary>US-CAT-03: несуществующий товар → 404.</summary>
        [Fact(DisplayName = "non_existent_product_returns_404")]
        public async Task non_existent_product_returns_404()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync($"/api/v1/catalog/products/{Guid.NewGuid()}");
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}