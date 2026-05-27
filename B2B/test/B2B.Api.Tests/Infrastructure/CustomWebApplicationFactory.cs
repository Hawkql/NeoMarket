using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Infrastructure.Persistence;
using FluentAssertions.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Testcontainers.PostgreSql;

namespace B2B.Api.Tests.Infrastructure
{

    /// <summary>
    /// Поднимает приложение в памяти + реальный Postgres в Docker (Testcontainers).
    /// Реальная БД важна — наши CHECK constraints, partial indexes, recursive CTE
    /// не работают на in-memory провайдере.
    /// </summary>
    public sealed class CustomWebApplicationFactory
        : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("b2b_test")
            .WithUsername("postgres")
            .WithPassword("test")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // ВАЖНО: ключ Jwt:SigningKey (как читает приложение), не Jwt:Secret
                    ["Jwt:SigningKey"] = "fantkfotpkgfkwotmalfjtofkglrtjfmfltjaptsj",
                    ["Jwt:Issuer"] = "neomarket-auth",
                    ["Jwt:Audience"] = "neomarket",
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["Jwt:RefreshTokenDays"] = "30",
                    // ServiceKey для тестов US-07/08/09/10 (X-Service-Key эндпоинты)
                    ["ServiceKey:Incoming"] = "test-service-key"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<B2BDbContext>));
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddDbContext<B2BDbContext>(options =>
                    options.UseNpgsql(_postgres.GetConnectionString()));
            });
        }

        // ОДИН InitializeAsync (убрать публичный дубль выше)
        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
            await db.Database.MigrateAsync();
            await SeedTestCategoryAsync(db);
        }

        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        private static async Task SeedTestCategoryAsync(B2BDbContext db)
        {
            var category = B2B.Domain.Categories.Category.Create(null, "Test Category",1);
            TestData.CategoryId = category.Id;
            db.Categories.Add(category);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    public static class TestData
    {
        public static Guid CategoryId { get; set; }
    }
}
