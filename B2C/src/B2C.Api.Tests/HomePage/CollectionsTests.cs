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
        /// US-CART-05: список подборок БЕЗ товаров (только метаданные).
        /// </summary>
        [Fact(DisplayName = "list_collections_returns_summary_without_products")]
        public async Task list_collections_returns_active_with_metadata()
        {
            var product1 = Guid.NewGuid();
            var product2 = Guid.NewGuid();
            var collection = await SeedCollectionAsync(
                slug: "hot-deals-test", title: "Хиты продаж",
                productIds: new[] { product1, product2 });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/home/collections");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> COLLECTIONS: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var found = false;
            foreach (var item in JsonDocument.Parse(body).RootElement.EnumerateArray())
            {
                if (item.GetProperty("id").GetGuid() == collection.Id)
                {
                    found = true;
                    item.GetProperty("title").GetString().Should().Be("Хиты продаж");
                    item.GetProperty("product_count").GetInt32().Should().Be(2);
                    // Товары в списке не выдаются — только summary.
                    item.TryGetProperty("products", out _).Should().BeFalse();
                }
            }
            found.Should().BeTrue();
        }

        /// <summary>
        /// US-CART-05: GET /home/collections/{slug} обогащает товары через B2B.
        /// Порядок ProductIds сохраняется.
        /// </summary>
        [Fact(DisplayName = "get_collection_enriches_and_preserves_order")]
        public async Task get_collection_returns_products_in_seeded_order()
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

            await SeedCollectionAsync(
                slug: "ordered-test", title: "Ordered",
                productIds: new[] { first, second, third });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/home/collections/ordered-test");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> COLLECTION DETAIL: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var products = JsonDocument.Parse(body).RootElement.GetProperty("products");
            products.GetArrayLength().Should().Be(3);

            // Порядок ProductIds сохранён.
            products[0].GetProperty("id").GetGuid().Should().Be(first);
            products[1].GetProperty("id").GetGuid().Should().Be(second);
            products[2].GetProperty("id").GetGuid().Should().Be(third);
        }

        /// <summary>
        /// US-CART-05: удалённые в B2B товары не попадают в items
        /// (как у Favorites — silent skip).
        /// </summary>
        [Fact(DisplayName = "collection_unavailable_products_in_unavailable_ids")]
        public async Task get_collection_returns_unavailable_ids_for_missing_products()
        {
            var existing = Guid.NewGuid();
            var ghost = Guid.NewGuid();

            _factory.CatalogFake.SeedProduct(new ProductSummary(
                existing, "Existing", null, 100_00, null, null, true, null, null));
            // ghost НЕ в B2B — имитация удалённого/заблокированного.

            await SeedCollectionAsync(
                slug: "with-missing-test", title: "Some",
                productIds: new[] { existing, ghost });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/home/collections/with-missing-test");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> COLLECTION WITH GHOST: {body}");

            var root = JsonDocument.Parse(body).RootElement;

            // Живой товар — в products.
            var products = root.GetProperty("products");
            products.GetArrayLength().Should().Be(1);
            products[0].GetProperty("id").GetGuid().Should().Be(existing);

            // Удалённый — в unavailable_ids.
            var unavailable = root.GetProperty("unavailable_ids");
            unavailable.GetArrayLength().Should().Be(1);
            unavailable[0].GetGuid().Should().Be(ghost);
        }

        /// <summary>US-CART-05: несуществующая подборка → 404.</summary>
        [Fact(DisplayName = "get_unknown_collection_returns_404")]
        public async Task unknown_slug_returns_404()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/home/collections/non-existent-slug-xyz");
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
    }
}