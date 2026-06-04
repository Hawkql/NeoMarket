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
    public sealed class CategoryNavigationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CategoryNavigationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.CatalogFake.Reset();
        }

        /// <summary>
        /// US-CAT-05: openapi GET /api/v1/catalog/categories — плоский список.
        /// </summary>
        [Fact(DisplayName = "categories_flat_returns_list")]
        public async Task categories_flat_returns_seeded_nodes()
        {
            var electronicsId = Guid.NewGuid();
            var phonesId = Guid.NewGuid();

            _factory.CatalogFake.SeedCategoryTree(new[]
            {
                new CategoryNode(electronicsId, ParentId: null, Name: "Electronics", Slug: "electronics",
                    Children: new[]
                    {
                        new CategoryNode(phonesId, electronicsId, "Phones", "phones",
                            Array.Empty<CategoryNode>())
                    }),
            });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/categories");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CATEGORIES FLAT: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var arr = JsonDocument.Parse(body).RootElement;

            // Плоский список — обе ноды (Electronics + Phones), без children.
            arr.GetArrayLength().Should().Be(2);

            // Electronics — level 0, path = ["Electronics"].
            var root = arr[0];
            root.GetProperty("name").GetString().Should().Be("Electronics");
            root.GetProperty("level").GetInt32().Should().Be(0);
            root.GetProperty("path").GetArrayLength().Should().Be(1);
            root.TryGetProperty("children", out _).Should().BeFalse(
                "плоский список не должен содержать children");

            // Phones — level 1, parent_id = electronicsId, path = ["Electronics", "Phones"].
            var phones = arr[1];
            phones.GetProperty("name").GetString().Should().Be("Phones");
            phones.GetProperty("level").GetInt32().Should().Be(1);
            phones.GetProperty("parent_id").GetGuid().Should().Be(electronicsId);
            phones.GetProperty("path").GetArrayLength().Should().Be(2);
        }

        /// <summary>
        /// US-CAT-05: openapi GET /api/v1/catalog/categories/tree — иерархия.
        /// </summary>
        [Fact(DisplayName = "categories_tree_returns_nested")]
        public async Task categories_tree_returns_seeded_nodes()
        {
            var electronicsId = Guid.NewGuid();
            var phonesId = Guid.NewGuid();

            _factory.CatalogFake.SeedCategoryTree(new[]
            {
                new CategoryNode(electronicsId, ParentId: null, Name: "Electronics", Slug: "electronics",
                    Children: new[]
                    {
                        new CategoryNode(phonesId, electronicsId, "Phones", "phones",
                            Array.Empty<CategoryNode>())
                    }),
            });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/categories/tree");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> TREE: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var root = JsonDocument.Parse(body).RootElement;
            root.GetArrayLength().Should().Be(1);
            root[0].GetProperty("name").GetString().Should().Be("Electronics");
            root[0].GetProperty("children").GetArrayLength().Should().Be(1);
            root[0].GetProperty("children")[0].GetProperty("name").GetString().Should().Be("Phones");
        }

        /// <summary>US-CAT-05: breadcrumbs по category_id.</summary>
        [Fact(DisplayName = "breadcrumbs_for_category")]
        public async Task breadcrumbs_returns_chain_from_root()
        {
            var rootCategoryId = Guid.NewGuid();
            var leafCategoryId = Guid.NewGuid();

            _factory.CatalogFake.SeedCategoryBreadcrumbs(leafCategoryId, new[]
            {
                new Breadcrumb(rootCategoryId, "Electronics", "electronics"),
                new Breadcrumb(leafCategoryId, "Phones", "phones"),
            });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync($"/api/v1/catalog/breadcrumbs?categoryId={leafCategoryId}");
            var body = await resp.Content.ReadAsStringAsync();

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var arr = JsonDocument.Parse(body).RootElement;
            arr.GetArrayLength().Should().Be(2);
            arr[0].GetProperty("name").GetString().Should().Be("Electronics");
            arr[1].GetProperty("name").GetString().Should().Be("Phones");
        }

        /// <summary>
        /// US-CAT-05: одновременно category_id и product_id → 400 (ambiguous_param).
        /// </summary>
        [Fact(DisplayName = "breadcrumbs_ambiguous_params_returns_400")]
        public async Task breadcrumbs_with_both_ids_returns_400()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync(
                $"/api/v1/catalog/breadcrumbs?categoryId={Guid.NewGuid()}&productId={Guid.NewGuid()}");

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Orphan-нода в дереве → 422 (US-CAT-05 acceptance).
        /// Теперь проверяется на /categories/tree (где живёт EnsureNoOrphans).
        /// </summary>
        [Fact(DisplayName = "category_tree_with_orphan_returns_422")]
        public async Task category_tree_with_orphan_returns_422()
        {
            var rootId = Guid.NewGuid();
            var orphanParentId = Guid.NewGuid();
            var orphanId = Guid.NewGuid();

            _factory.CatalogFake.SeedCategoryTree(new[]
            {
                new CategoryNode(rootId, ParentId: null, Name: "Electronics", Slug: "electronics",
                    Children: Array.Empty<CategoryNode>()),
                new CategoryNode(orphanId, ParentId: orphanParentId, Name: "Orphan", Slug: "orphan",
                    Children: Array.Empty<CategoryNode>()),
            });

            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/catalog/categories/tree");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> ORPHAN: {resp.StatusCode} BODY: {body}");

            resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }
}