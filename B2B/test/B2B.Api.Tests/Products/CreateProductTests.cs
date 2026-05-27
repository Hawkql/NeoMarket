using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using B2B.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;     
using Xunit;
namespace B2B.Api.Tests.Products
{
    [Collection("Sequential")]
    public sealed class CreateProductTests
    : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CreateProductTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateAuthorizedClient(Guid sellerId)
        {
            var client = _factory.CreateClient();
            var token = JwtTokenHelper.GenerateSellerToken(sellerId);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static object ValidProductBody() => new
        {
            category_id = TestData.CategoryId,
            title = "iPhone 15 Pro Max",
            description = "Флагман Apple",
            images = new[] { new { url = "/s3/iphone.jpg", ordering = 0 } },
            characteristics = new[] { new { name = "Бренд", value = "Apple" } }
        };

        [Fact]
        public async Task create_product_returns_201_with_created_status()
        {
            var client = CreateAuthorizedClient(Guid.NewGuid());

            var response = await client.PostAsJsonAsync(
                "/api/v1/products", ValidProductBody());
            var debugBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($">>> RESPONSE: {response.StatusCode} BODY: {debugBody}");
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("status").GetString()
                .Should().Be("CREATED");
            doc.RootElement.GetProperty("skus").GetArrayLength()
                .Should().Be(0);
        }

        [Fact]
        public async Task seller_id_taken_from_jwt()
        {
            var sellerId = Guid.NewGuid();
            var client = CreateAuthorizedClient(sellerId);
            var categoryId = await SeedCategoryAsync();   // ← свежая категория

            var bodyWithFakeSeller = new
            {
                seller_id = Guid.NewGuid(),               // IDOR-попытка, должна игнорироваться
                category_id = categoryId,
                title = "Test",
                description = "Test desc",
                images = new[] { new { url = "/s3/x.jpg", ordering = 0 } }
            };

            var response = await client.PostAsJsonAsync("/api/v1/products", bodyWithFakeSeller);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var json = await response.Content.ReadAsStringAsync();
            var productId = JsonDocument.Parse(json).RootElement.GetProperty("id").GetGuid();

            // Читаем СВЕЖИМ scope + AsNoTracking — без кэша
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<B2B.Infrastructure.Persistence.B2BDbContext>();

            var product = await db.Products
                 .AsNoTracking()
                 .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);

            product.Should().NotBeNull("товар должен быть создан под sellerId из JWT");
            product!.SellerId.Should().Be(sellerId);   // из JWT, не из тела
        }

        [Fact]
        public async Task missing_images_returns_400()
        {
            var client = CreateAuthorizedClient(Guid.NewGuid());

            var body = new
            {
                category_id = TestData.CategoryId,
                title = "Test",
                description = "Test desc",
                images = Array.Empty<object>()   // пустой массив
            };

            var response = await client.PostAsJsonAsync("/api/v1/products", body);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var json = await response.Content.ReadAsStringAsync();
            JsonDocument.Parse(json).RootElement.GetProperty("code").GetString()
                .Should().Be("INVALID_REQUEST");
        }
        [Fact(DisplayName = "invalid_category_id_returns_400")]
        public async Task invalid_category_id_returns_400()
        {
            var client = CreateAuthorizedClient(Guid.NewGuid());
            var body = new
            {
                category_id = Guid.NewGuid(),   // несуществующая категория
                title = "Test",
                description = "Test desc",
                images = new[] { new { url = "/s3/p.jpg", ordering = 0 } },
                characteristics = Array.Empty<object>()
            };
            var response = await client.PostAsJsonAsync("/api/v1/products", body);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var json = await response.Content.ReadAsStringAsync();
            JsonDocument.Parse(json).RootElement.GetProperty("code").GetString()
                .Should().Be("INVALID_REQUEST");
        }
        
        [Fact]
        public async Task missing_category_returns_400()
        {
            var client = CreateAuthorizedClient(Guid.NewGuid());

            var body = new
            {
                // category_id отсутствует → Guid.Empty → валидатор отклонит
                title = "Test",
                description = "Test desc",
                images = new[] { new { url = "/s3/x.jpg", ordering = 0 } }
            };

            var response = await client.PostAsJsonAsync("/api/v1/products", body);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        private async Task<Guid> SeedCategoryAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<B2B.Infrastructure.Persistence.B2BDbContext>();
            var category = B2B.Domain.Categories.Category.Create(null, "Test Category",1);
            db.Categories.Add(category);
            await db.SaveChangesAsync(CancellationToken.None);
            return category.Id;
        }
    }
}
