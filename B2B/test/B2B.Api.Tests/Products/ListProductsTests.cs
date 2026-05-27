using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using B2B.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace B2B.Api.Tests.Products
{
    [Collection("Sequential")]
    public sealed class ListProductsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ListProductsTests(CustomWebApplicationFactory factory) => _factory = factory;

        private HttpClient AuthClient(Guid sellerId)
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return c;
        }

        private async Task<Guid> CreateProductAsync(HttpClient client, string title)
        {
            var body = new
            {
                category_id = TestData.CategoryId,
                title,
                description = "desc",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var resp = await client.PostAsJsonAsync("/api/v1/products", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            return JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                .RootElement.GetProperty("id").GetGuid();
        }

        private static List<Guid> Ids(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("items").EnumerateArray()
                .Select(e => e.GetProperty("id").GetGuid()).ToList();
        }

        // ── продавец видит только свои товары ──
        [Fact(DisplayName = "list_returns_only_own_products")]
        public async Task list_returns_only_own_products()
        {
            var sellerA = Guid.NewGuid();
            var sellerB = Guid.NewGuid();
            var clientA = AuthClient(sellerA);
            var clientB = AuthClient(sellerB);

            var aProduct = await CreateProductAsync(clientA, "A product");
            var bProduct = await CreateProductAsync(clientB, "B product");

            var json = await (await clientA.GetAsync("/api/v1/products")).Content.ReadAsStringAsync();
            var ids = Ids(json);

            ids.Should().Contain(aProduct);
            ids.Should().NotContain(bProduct, "продавец не видит чужие товары");
        }

        // ── seller_id в query игнорируется (IDOR) ──
        [Fact(DisplayName = "idor_query_param_seller_id_ignored")]
        public async Task idor_query_param_seller_id_ignored()
        {
            var sellerA = Guid.NewGuid();
            var sellerB = Guid.NewGuid();
            var aProduct = await CreateProductAsync(AuthClient(sellerA), "A own");
            var bProduct = await CreateProductAsync(AuthClient(sellerB), "B own");

            // A пытается подменить seller_id на B через query
            var json = await (await AuthClient(sellerA)
                .GetAsync($"/api/v1/products?seller_id={sellerB}")).Content.ReadAsStringAsync();
            var ids = Ids(json);

            ids.Should().Contain(aProduct, "seller_id берётся из JWT, query игнорируется");
            ids.Should().NotContain(bProduct, "подмена seller_id не даёт доступ к чужим");
        }

        // ── удалённые видны при include_deleted с флагом ──
        [Fact(DisplayName = "deleted_products_visible_with_deleted_flag")]
        public async Task deleted_products_visible_with_deleted_flag()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client, "To delete");
            await client.DeleteAsync($"/api/v1/products/{productId}");

            // без флага — не виден
            var without = Ids(await (await client.GetAsync("/api/v1/products"))
                .Content.ReadAsStringAsync());
            without.Should().NotContain(productId);

            // с флагом — виден, deleted=true
            var withJson = await (await client.GetAsync("/api/v1/products?include_deleted=true"))
                .Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(withJson);
            var item = doc.RootElement.GetProperty("items").EnumerateArray()
                .FirstOrDefault(e => e.GetProperty("id").GetGuid() == productId);
            item.ValueKind.Should().NotBe(JsonValueKind.Undefined, "удалённый виден при include_deleted");
            item.GetProperty("deleted").GetBoolean().Should().BeTrue();
        }

        // ── фильтр по статусу ──
        [Fact(DisplayName = "status_filter_works_correctly")]
        public async Task status_filter_works_correctly()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);

            // товар останется CREATED (без SKU)
            var createdProduct = await CreateProductAsync(client, "Created one");

            // фильтр status=CREATED → виден
            var createdJson = await (await client.GetAsync("/api/v1/products?status=CREATED"))
                .Content.ReadAsStringAsync();
            Ids(createdJson).Should().Contain(createdProduct);

            // фильтр status=MODERATED → не виден (он CREATED)
            var moderatedJson = await (await client.GetAsync("/api/v1/products?status=MODERATED"))
                .Content.ReadAsStringAsync();
            Ids(moderatedJson).Should().NotContain(createdProduct);
        }

        // ── поиск по названию без учёта регистра ──
        [Fact(DisplayName = "search_by_title_case_insensitive")]
        public async Task search_by_title_case_insensitive()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);

            var iphone = await CreateProductAsync(client, "iPhone 15 Pro");
            var samsung = await CreateProductAsync(client, "Samsung Galaxy");

            // поиск в нижнем регистре должен найти "iPhone"
            var json = await (await client.GetAsync("/api/v1/products?search=iphone"))
                .Content.ReadAsStringAsync();
            var ids = Ids(json);

            ids.Should().Contain(iphone, "поиск регистронезависимый");
            ids.Should().NotContain(samsung);
        }
    }
}
