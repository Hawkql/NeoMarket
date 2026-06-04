using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Api.Tests.Infrastructure;
using B2C.Application.Integration.Dtos;
using B2C.Domain.HomePage;
using B2C.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2C.Api.Tests.HomePage
{
    [Collection("Sequential")]
    public sealed class CollectionsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CollectionsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
        }

        /// <summary>
        /// US-CART-05: openapi GET /api/v1/catalog/collections — каждая подборка
        /// уже содержит обогащённый products (CatalogProductCard).
        /// </summary>
        [Fact(DisplayName = "list_collections_returns_collections_with_products")]
        public async Task list_collections_returns_active_with_products()
        {
            var product1 = Guid.NewGuid();
            var product2 = Guid.NewGuid();
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                product1, "Product 1", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                product2, "Product 2", null, 200_00, null, null, true, null, null));

            var collection = await SeedCollectionAsync(
                slug: "hot-deals-test", title: "Хиты продаж",
                productIds: new[] { product1, product2 });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/collections");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> COLLECTIONS: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var found = false;
            foreach (var item in JsonDocument.Parse(body).RootElement.EnumerateArray())
            {
                if (item.GetProperty("id").GetGuid() == collection.Id)
                {
                    found = true;
                    item.GetProperty("name").GetString().Should().Be("Хиты продаж");
                    var products = item.GetProperty("products");
                    products.GetArrayLength().Should().Be(2);
                }
            }
            found.Should().BeTrue();
        }

        /// <summary>
        /// US-CART-05: порядок ProductIds внутри подборки сохраняется.
        /// </summary>
        [Fact(DisplayName = "collection_products_preserve_seeded_order")]
        public async Task collection_products_preserve_seeded_order()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            var third = Guid.NewGuid();

            _factory.CatalogFake.SeedProduct(new ProductSummary(
                first, "First", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                second, "Second", null, 200_00, null, null, true, null, null));
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                third, "Third", null, 300_00, null, null, true, null, null));

            var collection = await SeedCollectionAsync(
                slug: "ordered-test", title: "Ordered",
                productIds: new[] { first, second, third });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/collections");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> COLLECTION ORDER: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            JsonElement? mine = null;
            foreach (var item in JsonDocument.Parse(body).RootElement.EnumerateArray())
                if (item.GetProperty("id").GetGuid() == collection.Id)
                    mine = item;

            mine.Should().NotBeNull();
            var products = mine!.Value.GetProperty("products");
            products.GetArrayLength().Should().Be(3);
            products[0].GetProperty("id").GetGuid().Should().Be(first);
            products[1].GetProperty("id").GetGuid().Should().Be(second);
            products[2].GetProperty("id").GetGuid().Should().Be(third);
        }

        /// <summary>
        /// US-CART-05: удалённые в B2B товары просто не попадают в products.
        /// (openapi не предусматривает поле unavailable_ids — silent skip.)
        /// </summary>
        [Fact(DisplayName = "collection_skips_unavailable_products")]
        public async Task collection_skips_products_missing_in_b2b()
        {
            var existing = Guid.NewGuid();
            var ghost = Guid.NewGuid();

            _factory.CatalogFake.SeedProduct(new ProductSummary(
                existing, "Existing", null, 100_00, null, null, true, null, null));
            // ghost НЕ в B2B — имитация удалённого/заблокированного.

            var collection = await SeedCollectionAsync(
                slug: "with-missing-test", title: "Some",
                productIds: new[] { existing, ghost });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/collections");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> COLLECTION WITH GHOST: {body}");

            JsonElement? mine = null;
            foreach (var item in JsonDocument.Parse(body).RootElement.EnumerateArray())
                if (item.GetProperty("id").GetGuid() == collection.Id)
                    mine = item;

            mine.Should().NotBeNull();
            var products = mine!.Value.GetProperty("products");
            products.GetArrayLength().Should().Be(1);
            products[0].GetProperty("id").GetGuid().Should().Be(existing);
        }

        /// <summary>
        /// Seed подборки напрямую через DbContext.
        /// </summary>
        private async Task<Collection> SeedCollectionAsync(
            string slug, string title, Guid[] productIds)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();

            var collection = Collection.Create(
                slug: slug,
                title: title,
                description: null,
                coverImageUrl: null,
                priority: 0,
                productIds: productIds);

            db.Collections.Add(collection);
            await db.SaveChangesAsync();
            return collection;
        }
        /// <summary>
        /// Расширение: GET /api/v1/catalog/collections/{id} — детальная коллекция.
        /// Адресация по id согласно требованию ревьюера.
        /// </summary>
        [Fact(DisplayName = "get_collection_by_id_returns_enriched")]
        public async Task get_collection_by_id_returns_products_in_order()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                first, "First", null, 100_00, null, null, true, null, null));
            _factory.CatalogFake.SeedProduct(new ProductSummary(
                second, "Second", null, 200_00, null, null, true, null, null));

            var collection = await SeedCollectionAsync(
                slug: "by-id-test", title: "By ID",
                productIds: new[] { first, second });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync($"/api/v1/catalog/collections/{collection.Id}");
            var body = await resp.Content.ReadAsStringAsync();

            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var root = JsonDocument.Parse(body).RootElement;
            root.GetProperty("name").GetString().Should().Be("By ID");
            var products = root.GetProperty("products");
            products.GetArrayLength().Should().Be(2);
            products[0].GetProperty("id").GetGuid().Should().Be(first);
            products[1].GetProperty("id").GetGuid().Should().Be(second);
        }

        /// <summary>Несуществующая коллекция → 404.</summary>
        [Fact(DisplayName = "get_unknown_collection_returns_404")]
        public async Task get_unknown_collection_by_id_returns_404()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync($"/api/v1/catalog/collections/{Guid.NewGuid()}");
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}