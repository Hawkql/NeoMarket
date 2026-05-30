using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;       // ← здесь лежит IB2BCatalogClient и IB2BReservationClient
using B2C.Domain.HomePage;
using B2C.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
namespace B2C.Api.Tests.Infrastructure
{
    public sealed class CustomWebApplicationFactory
        : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("b2c_test")
            .WithUsername("postgres")
            .WithPassword("test")
            .Build();

        // Фейки как поля — чтобы тесты могли достать их через factory.CatalogFake / ReservationFake.
        // Singleton-инстансы регистрируются в DI, и эти же ссылки доступны напрямую.
        public FakeB2BCatalogClient CatalogFake { get; } = new();
        public FakeB2BReservationClient ReservationFake { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // JWT — секрет совпадает с JwtTokenHelper.Secret.
                    ["Jwt:Secret"] = "b2c-jwt-signing-key-must-be-at-least-32-chars-long-test",
                    ["Jwt:Issuer"] = "neomarket-b2c",
                    ["Jwt:Audience"] = "neomarket",
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["Jwt:RefreshTokenDays"] = "30",

                    // X-Service-Key для приёма от B2B (B2BEventsController).
                    ["ServiceKey:Incoming"] = "test-incoming-service-key",

                    // Outbound на B2B — не используется (HTTP-клиенты заменены фейками),
                    // но конфиг должен парситься без падений.
                    ["B2BClient:BaseUrl"] = "http://localhost:0",
                    ["B2BClient:ServiceKey"] = "test-b2b-key",

                    // Миграции запустим вручную в InitializeAsync.
                    ["ApplyMigrationsOnStartup"] = "false",
                });
            });

            builder.ConfigureTestServices(services =>
            {
                // 1. DbContext → тестовая БД из контейнера.
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<B2CDbContext>));
                if (dbDescriptor is not null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<B2CDbContext>(options =>
                    options.UseNpgsql(_postgres.GetConnectionString()));

                // 2. B2B-клиенты → фейки (singleton — общее состояние тестов и handler'ов).
                RemoveAll(services, typeof(IB2BCatalogClient));
                RemoveAll(services, typeof(IB2BReservationClient));
                services.AddSingleton<IB2BCatalogClient>(CatalogFake);
                services.AddSingleton<IB2BReservationClient>(ReservationFake);

                // 3. Убираем фоновые HostedService — Outbox/CancelPendingRetry/FulfillRetry.
                //    В тестах они только путают timing. Retry-сценарии проверяем прямой отправкой
                //    команд RetryUnreserveCommand/CompleteFulfillCommand через MediatR.
                RemoveAll(services, typeof(IHostedService));
            });
        }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            await db.Database.MigrateAsync();
            await SeedBaseDataAsync(db);
        }

        public new async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        /// <summary>
        /// Базовые данные, переиспользуемые тестами через TestData.
        /// Конкретные сущности (заказы, корзины) создаются в каждом тесте отдельно.
        /// </summary>
        private static async Task SeedBaseDataAsync(B2CDbContext db)
        {
            // Активный баннер — для тестов US-CART-04 (если будут).
            var banner = Banner.Create(
                title: "Test Banner",
                imageUrl: "/img/test.jpg",
                linkUrl: null,
                priority: 100,
                startsAt: null,
                endsAt: null);

            TestData.BannerId = banner.Id;
            db.Banners.Add(banner);

            await db.SaveChangesAsync(CancellationToken.None);
        }
        public void EnsureBuyer(Guid buyerId)
        {
            EnsureBuyerAsync(buyerId).GetAwaiter().GetResult();
        }

        public async Task EnsureBuyerAsync(Guid buyerId)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<B2CDbContext>();
            var now = DateTime.UtcNow;
            var email = $"buyer-{buyerId:N}@test.local";

            // ON CONFLICT — идемпотентно, тест может вызвать дважды.
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO buyers (id, email, password_hash, deleted, created_at, updated_at)
                VALUES ({buyerId}, {email}, 'test-hash', false, {now}, {now})
                ON CONFLICT (id) DO NOTHING");
        }
        private static void RemoveAll(IServiceCollection services, Type serviceType)
        {
            var toRemove = services.Where(s => s.ServiceType == serviceType).ToList();
            foreach (var d in toRemove) services.Remove(d);
        }
    }

    /// <summary>
    /// Хранилище ID базовых seed-сущностей. Заполняется в CustomWebApplicationFactory.SeedBaseDataAsync,
    /// читается тестами. По канону B2B (см. TestData.CategoryId).
    /// </summary>
    public static class TestData
    {
        public static Guid BannerId { get; set; }
    }
}
