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
    public sealed class CategoryFiltersTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CategoryFiltersTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
        }

        /// <summary>
        /// US-CAT-01: GET /categories/{id}/filters возвращает доступные фильтры
        /// (бренд, цвет, цена) с возможными значениями и счётчиками.
        /// </summary>
        [Fact(DisplayName = "category_filters_returns_definitions_with_counts")]
        public async Task filters_returned_with_facet_counts()
        {
            var categoryId = Guid.NewGuid();

            _factory.CatalogFake.SeedCategoryFilters(categoryId, new CategoryFilters(
                Filters: new[]
                {
                    new FilterDefinition("brand", "Бренд", new[]
                    {
                        new FilterValue("Apple", Count: 24),
                        new FilterValue("Samsung", Count: 12),
                    }),
                },
                PriceMin: 10_00,
                PriceMax: 500_00));

            var client = _factory.CreateClient();
            var resp = await client.GetAsync($"/api/v1/catalog/categories/{categoryId}/filters");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> FILTERS: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var root = JsonDocument.Parse(body).RootElement;

            root.GetProperty("price_min").GetInt32().Should().Be(10_00);
            root.GetProperty("price_max").GetInt32().Should().Be(500_00);

            var brand = root.GetProperty("filters")[0];
            brand.GetProperty("slug").GetString().Should().Be("brand");
            var values = brand.GetProperty("values");
            values.GetArrayLength().Should().Be(2);
            values[0].GetProperty("count").GetInt32().Should().Be(24);
        }
    }
}