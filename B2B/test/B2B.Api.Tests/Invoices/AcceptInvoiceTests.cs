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
    public sealed class AcceptInvoiceTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AcceptInvoiceTests(CustomWebApplicationFactory factory) => _factory = factory;

        private HttpClient SellerClient(Guid sellerId)
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateSellerToken(sellerId));
            return c;
        }

        private HttpClient AdminClient()
        {
            var c = _factory.CreateClient();
            c.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateAdminToken(Guid.NewGuid()));
            return c;
        }

        // Создаёт продавца, товар (Moderated), SKU и накладную — возвращает invoiceId
        private async Task<Guid> CreateInvoiceForSellerAsync(Guid sellerId)
        {
            var seller = SellerClient(sellerId);

            // продукт
            var prod = new
            {
                category_id = TestData.CategoryId,
                title = "P",
                description = "d",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var pid = JsonDocument.Parse(await (await seller.PostAsJsonAsync("/api/v1/products", prod))
                .Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

            // SKU
            var sku = new
            {
                product_id = pid,
                name = "S",
                price = 1000000,
                discount = 0,
                cost_price = 500000,
                article = "A",
                images = new[] { new { url = "/s3/s.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var skuId = JsonDocument.Parse(await (await seller.PostAsJsonAsync("/api/v1/skus", sku))
                .Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

            // Approve товара
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
                var product = await db.Products
                    .Include(p => p.FieldReports)
                    .FirstAsync(p => p.Id == pid);
                product.Approve();
                await db.SaveChangesAsync();
            }

            // Накладная
            var invBody = new { items = new[] { new { sku_id = skuId, quantity = 10 } } };
            var invResp = await seller.PostAsJsonAsync("/api/v1/invoices", invBody);
            invResp.StatusCode.Should().Be(HttpStatusCode.Created);
            return JsonDocument.Parse(await invResp.Content.ReadAsStringAsync())
                .RootElement.GetProperty("id").GetGuid();
        }

        // ── admin принимает любую накладную → 200, статус ACCEPTED ──
        [Fact(DisplayName = "admin_can_accept_invoice")]
        public async Task admin_can_accept_invoice()
        {
            var sellerId = Guid.NewGuid();
            var invoiceId = await CreateInvoiceForSellerAsync(sellerId);

            var resp = await AdminClient().PostAsJsonAsync(
                $"/api/v1/invoices/{invoiceId}/accept", new { accepted_items = Array.Empty<object>() });
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var status = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                .RootElement.GetProperty("status").GetString();
            status.Should().Be("ACCEPTED", "приёмка без частичных — полностью принято");
        }

        // ── seller (даже владелец накладной) не может принять → 403 ──
        [Fact(DisplayName = "seller_cannot_accept_invoice_returns_403")]
        public async Task seller_cannot_accept_invoice_returns_403()
        {
            var sellerId = Guid.NewGuid();
            var invoiceId = await CreateInvoiceForSellerAsync(sellerId);

            // тот же продавец-владелец пытается принять
            var resp = await SellerClient(sellerId).PostAsJsonAsync(
                $"/api/v1/invoices/{invoiceId}/accept", new { accepted_items = Array.Empty<object>() });

            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "приёмку выполняет только админ, не продавец");
        }

        // ── без токена → 401 ──
        [Fact(DisplayName = "unauthenticated_accept_returns_401")]
        public async Task unauthenticated_accept_returns_401()
        {
            var sellerId = Guid.NewGuid();
            var invoiceId = await CreateInvoiceForSellerAsync(sellerId);

            var noAuth = _factory.CreateClient();
            var resp = await noAuth.PostAsJsonAsync(
                $"/api/v1/invoices/{invoiceId}/accept", new { accepted_items = Array.Empty<object>() });

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}