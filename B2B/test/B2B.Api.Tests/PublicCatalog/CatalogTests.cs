using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using B2B.Api.Tests.Infrastructure;
using B2B.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using B2B.Domain.Products;
using Xunit;

namespace B2B.Api.Tests.PublicCatalog
{
    [Collection("Sequential")]
    public sealed class CatalogTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string ServiceKey = "test-service-key";   // = ServiceKey:Incoming в тестовом конфиге
        private readonly CustomWebApplicationFactory _factory;

        public CatalogTests(CustomWebApplicationFactory factory) => _factory = factory;

        private HttpClient SellerClient(Guid sellerId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return client;
        }

        private HttpClient ServiceClient()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Service-Key", ServiceKey);
            return client;
        }

        private async Task<Guid> CreateProductAsync(HttpClient client)
        {
            var body = new
            {
                category_id = TestData.CategoryId,
                title = "Catalog Product",
                description = "desc",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var resp = await client.PostAsJsonAsync("/api/v1/products", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            return JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                .RootElement.GetProperty("id").GetGuid();
        }

        private async Task<Guid> CreateSkuAsync(HttpClient client, Guid productId)
        {
            var body = new
            {
                product_id = productId,
                name = "SKU-1",
                price = 1000000,
                discount = 0,
                cost_price = 500000,
                article = "ART-1",
                images = new[] { new { url = "/s3/sku.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var resp = await client.PostAsJsonAsync("/api/v1/skus", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            return JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                .RootElement.GetProperty("id").GetGuid();
        }

        // Moderated + остаток на складе
        private async Task MakeVisibleAsync(Guid productId, Guid skuId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var product = await db.Products.Include(p => p.FieldReports).FirstAsync(p => p.Id == productId);
            product.Approve();   // → Moderated
            var sku = await db.Skus.FirstAsync(s => s.Id == skuId);
            sku.IncreaseStock(10);   // ActiveQuantity > 0
            await db.SaveChangesAsync();
        }

        private async Task HardBlockAsync(Guid productId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var product = await db.Products.Include(p => p.FieldReports).FirstAsync(p => p.Id == productId);
            product.HardBlock(
                new BlockingReason(Guid.NewGuid(), "x"),
                new List<(FieldReportTarget, Guid?, string)>(),
                new List<Guid>(),
                DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        // ── каталог отдаёт MODERATED + в наличии ──
        [Fact(DisplayName = "catalog_returns_moderated_in_stock_products")]
        public async Task catalog_returns_moderated_in_stock_products()
        {
            var seller = SellerClient(Guid.NewGuid());
            var productId = await CreateProductAsync(seller);
            var skuId = await CreateSkuAsync(seller, productId);
            await MakeVisibleAsync(productId, skuId);

            var resp = await ServiceClient().GetAsync($"/api/v1/products?ids={productId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var ids = arr.EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList();
            ids.Should().Contain(productId);
        }

        // ── HARD_BLOCKED исключены ──
        [Fact(DisplayName = "catalog_excludes_hard_blocked")]
        public async Task catalog_excludes_hard_blocked()
        {
            var seller = SellerClient(Guid.NewGuid());
            var productId = await CreateProductAsync(seller);
            var skuId = await CreateSkuAsync(seller, productId);
            await MakeVisibleAsync(productId, skuId);
            await HardBlockAsync(productId);   // → HardBlocked

            var resp = await ServiceClient().GetAsync($"/api/v1/products?ids={productId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var ids = arr.EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList();
            ids.Should().NotContain(productId, "HARD_BLOCKED не попадает в витрину");
        }

        // ── без X-Service-Key → 401 ──
        [Fact(DisplayName = "catalog_missing_service_key_returns_401")]
        public async Task catalog_missing_service_key_returns_401()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/v1/public/products");

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Тело должно быть плоским {code, message} (US-07 fix)
            var json = await resp.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json).RootElement;
            root.GetProperty("code").GetString().Should().Be("UNAUTHORIZED");
            root.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
        }

        // ── в ответе нет cost_price ──
        [Fact(DisplayName = "catalog_response_has_no_cost_price")]
        public async Task catalog_response_has_no_cost_price()
        {
            var seller = SellerClient(Guid.NewGuid());
            var productId = await CreateProductAsync(seller);
            var skuId = await CreateSkuAsync(seller, productId);
            await MakeVisibleAsync(productId, skuId);

            var resp = await ServiceClient().GetAsync($"/api/v1/products?ids={productId}");
            var raw = await resp.Content.ReadAsStringAsync();

            raw.Should().NotContain("cost_price", "себестоимость не раскрывается в витрине B2C");
        }

        // ── ?ids= возвращает только видимое подмножество ──
        [Fact(DisplayName = "batch_ids_returns_visible_subset")]
        public async Task batch_ids_returns_visible_subset()
        {
            var seller = SellerClient(Guid.NewGuid());

            // видимый
            var visibleId = await CreateProductAsync(seller);
            var visSku = await CreateSkuAsync(seller, visibleId);
            await MakeVisibleAsync(visibleId, visSku);

            // невидимый (остался CREATED, без SKU)
            var hiddenId = await CreateProductAsync(seller);

            var resp = await ServiceClient()
                .GetAsync($"/api/v1/products?ids={visibleId},{hiddenId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var ids = arr.EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList();

            ids.Should().Contain(visibleId);
            ids.Should().NotContain(hiddenId, "невидимые товары не возвращаются даже при запросе по id");
        }
        
    }
}
