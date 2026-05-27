using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using B2B.Api.Tests.Infrastructure;
using B2B.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2B.Api.Tests.Invoices
{
    [Collection("Sequential")]
    public sealed class CreateInvoiceTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CreateInvoiceTests(CustomWebApplicationFactory factory) => _factory = factory;

        private HttpClient AuthClient(Guid sellerId)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return client;
        }

        private async Task<Guid> CreateProductAsync(HttpClient client)
        {
            var body = new
            {
                category_id = TestData.CategoryId,
                title = "Inv Product",
                description = "desc",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var resp = await client.PostAsJsonAsync("/api/v1/products", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
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
            var json = await resp.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();
        }

        // Доводит товар до Moderated (первый SKU → OnModeration, затем Approve)
        private async Task ApproveProductAsync(Guid productId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            var product = await db.Products
                .Include(p => p.FieldReports)
                .FirstAsync(p => p.Id == productId);
            product.Approve();
            await db.SaveChangesAsync();
        }

        // ── happy: накладная на MODERATED-SKU → 201, статус PENDING ──
        [Fact(DisplayName = "create_invoice_with_moderated_sku_returns_201")]
        public async Task create_invoice_with_moderated_sku_returns_201()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);
            var skuId = await CreateSkuAsync(client, productId);  // → OnModeration
            await ApproveProductAsync(productId);                 // → Moderated

            var body = new { items = new[] { new { sku_id = skuId, quantity = 10 } } };
            var resp = await client.PostAsJsonAsync("/api/v1/invoices", body);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);

            var json = await resp.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json).RootElement;
            root.GetProperty("status").GetString().Should().Be("PENDING");
            root.GetProperty("items").GetArrayLength().Should().Be(1);
        }

        // ── пустой items → 400 ──
        [Fact(DisplayName = "empty_items_returns_400")]
        public async Task empty_items_returns_400()
        {
            var client = AuthClient(Guid.NewGuid());
            var body = new { items = Array.Empty<object>() };
            var resp = await client.PostAsJsonAsync("/api/v1/invoices", body);
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ── SKU товара не в MODERATED → 400 ──
        [Fact(DisplayName = "non_moderated_sku_returns_400")]
        public async Task non_moderated_sku_returns_400()
        {
            var sellerId = Guid.NewGuid();
            var client = AuthClient(sellerId);
            var productId = await CreateProductAsync(client);
            var skuId = await CreateSkuAsync(client, productId);  // товар → OnModeration (НЕ Moderated)

            var body = new { items = new[] { new { sku_id = skuId, quantity = 5 } } };
            var resp = await client.PostAsJsonAsync("/api/v1/invoices", body);

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var json = await resp.Content.ReadAsStringAsync();
            JsonDocument.Parse(json).RootElement.GetProperty("code").GetString()
                .Should().Be("INVALID_REQUEST");
        }

        // ── чужой SKU → 403 NOT_OWNER ──
        [Fact(DisplayName = "others_sku_returns_403")]
        public async Task others_sku_returns_403()
        {
            // Владелец создаёт товар + SKU, доводит до Moderated
            var ownerId = Guid.NewGuid();
            var ownerClient = AuthClient(ownerId);
            var productId = await CreateProductAsync(ownerClient);
            var skuId = await CreateSkuAsync(ownerClient, productId);
            await ApproveProductAsync(productId);

            // Другой продавец пытается выставить накладную на чужой SKU
            var attacker = AuthClient(Guid.NewGuid());
            var body = new { items = new[] { new { sku_id = skuId, quantity = 1 } } };
            var resp = await attacker.PostAsJsonAsync("/api/v1/invoices", body);

            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var json = await resp.Content.ReadAsStringAsync();
            JsonDocument.Parse(json).RootElement.GetProperty("code").GetString()
                .Should().Be("NOT_OWNER");
        }
    }
}
