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
    public sealed class SimilarProductsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public SimilarProductsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
        }

        /// <summary>
        /// US-CAT-04: похожие — из той же категории, текущий товар исключён.
        /// </summary>
        [Fact(DisplayName = "similar_excludes_current_product")]
        public async Task similar_returns_products_from_same_category_excluding_self()
        {
            var categoryId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var other1 = Guid.NewGuid();
            var other2 = Guid.NewGuid();

            foreach (var id in new[] { targetId, other1, other2 })
            {
                _factory.CatalogFake.SeedProductInList(new ProductSummary(
                    id, $"Product-{id:N}", null, 100_00, null, null, true, null, null));
                _factory.CatalogFake.MapProductToCategory(id, categoryId);
            }

            var client = _factory.CreateClient();
            var resp = await client.GetAsync($"/api/v1/catalog/products/{targetId}/similar?limit=5");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> SIMILAR: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var items = JsonDocument.Parse(body).RootElement;
            items.GetArrayLength().Should().Be(2);

            // Текущий товар не в списке.
            foreach (var item in items.EnumerateArray())
                item.GetProperty("id").GetGuid().Should().NotBe(targetId);
        }

        /// <summary>US-CAT-04: если похожих нет — пустой массив (не ошибка).</summary>
        [Fact(DisplayName = "no_similar_returns_empty_array")]
        public async Task no_similar_returns_empty()
        {
            var categoryId = Guid.NewGuid();
            var loneProductId = Guid.NewGuid();
            _factory.CatalogFake.SeedProductInList(new ProductSummary(
                loneProductId, "Lone", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.MapProductToCategory(loneProductId, categoryId);

            var client = _factory.CreateClient();
            var resp = await client.GetAsync($"/api/v1/catalog/products/{loneProductId}/similar?limit=5");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                .RootElement.GetArrayLength().Should().Be(0);
        }
    }
}