using System;
using B2C.Application.Common.Abstractions;
using B2C.Application.Common.Interface;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;
using B2C.Domain.Addresses;
using B2C.Domain.Buyers;
using B2C.Domain.Carts;
using B2C.Domain.Favorites;
using B2C.Domain.HomePage;
using B2C.Domain.Orders;
using B2C.Domain.Subscriptions;
using B2C.Infrastructure.BackgroundJobs;
using B2C.Infrastructure.Integration;
using B2C.Infrastructure.Outbox;
using B2C.Infrastructure.Outbox.Dispatchers;
using B2C.Infrastructure.Persistence;

using B2C.Infrastructure.Persistence.Repositories;
using B2C.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace B2C.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration config)
        {
            AddPersistence(services, config);
            AddRepositories(services);
            AddSecurity(services, config);
            AddB2BIntegration(services, config);
            AddOutbox(services);
            AddBackgroundJobs(services);
            return services;
        }

        // ========================================================================
        // 1. PERSISTENCE (DbContext + UnitOfWork + TransactionManager + IdempotencyStore)
        // ========================================================================
        private static void AddPersistence(IServiceCollection services, IConfiguration config)
        {
            var connectionString = config.GetConnectionString("B2CDatabase")
                ?? throw new InvalidOperationException(
                    "Connection string 'B2CDatabase' not configured");

            services.AddDbContext<B2CDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(B2CDbContext).Assembly.FullName);
                });
            });

            services.AddScoped<ITransactionManager, TransactionManager>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        }

        // ========================================================================
        // 2. REPOSITORIES (все 10)
        // ========================================================================
        private static void AddRepositories(IServiceCollection services)
        {
            services.AddScoped<IBuyerRepository, BuyerRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IFavoriteRepository, FavoriteRepository>();
            services.AddScoped<IProductSubscriptionRepository, ProductSubscriptionRepository>();
            services.AddScoped<IBannerRepository, BannerRepository>();
            services.AddScoped<IBannerEventRepository, BannerEventRepository>();
            services.AddScoped<ICollectionRepository, CollectionRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
        }

        // ========================================================================
        // 3. SECURITY (BCrypt + JWT — свой секрет B2C, per-service auth)
        // ========================================================================
        private static void AddSecurity(IServiceCollection services, IConfiguration config)
        {
            services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            services.AddSingleton<ITokenService, TokenService>();
        }

        // ========================================================================
        // 4. B2B INTEGRATION (typed HTTP-клиенты с X-Service-Key)
        // ========================================================================
        private static void AddB2BIntegration(IServiceCollection services, IConfiguration config)
        {
            services.Configure<B2BClientOptions>(config.GetSection(B2BClientOptions.SectionName));

            // Общий конфигуратор HttpClient: BaseUrl + X-Service-Key + timeout.
            // X-Service-Key проставляется здесь, в DefaultRequestHeaders — не в каждом методе клиента.
            static void ConfigureB2BClient(IServiceProvider sp, System.Net.Http.HttpClient http)
            {
                var options = sp.GetRequiredService<IOptions<B2BClientOptions>>().Value;
                http.BaseAddress = new Uri(options.BaseUrl);
                http.Timeout = TimeSpan.FromSeconds(15);
                http.DefaultRequestHeaders.Add("X-Service-Key", options.ServiceKey);
            }

            // Каталог (read-операции).
            services.AddHttpClient<IB2BCatalogClient, B2BCatalogHttpClient>(ConfigureB2BClient);

            // Резервирование (reserve/unreserve/fulfill).
            services.AddHttpClient<IB2BReservationClient, B2BReservationHttpClient>(ConfigureB2BClient);
        }

        // ========================================================================
        // 5. OUTBOX (Mapper + Dispatchers + BackgroundService)
        //    Сейчас маппер пуст (B2C синхронен с B2B), но инфраструктура готова.
        // ========================================================================
        private static void AddOutbox(IServiceCollection services)
        {
            services.AddScoped<IIntegrationEventMapper, IntegrationEventMapper>();

            // HTTP-клиент диспетчера B2B (использует те же B2BClientOptions).
            services.AddHttpClient<HttpB2bDispatcher>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<B2BClientOptions>>().Value;
                http.BaseAddress = new Uri(options.BaseUrl);
                http.Timeout = TimeSpan.FromSeconds(10);
                http.DefaultRequestHeaders.Add("X-Service-Key", options.ServiceKey);
            });

            // Регистрируем диспетчер под общим интерфейсом для DispatcherRegistry.
            services.AddScoped<IIntegrationEventDispatcher>(sp =>
                sp.GetRequiredService<HttpB2bDispatcher>());

            services.AddScoped<DispatcherRegistry>();

            services.AddHostedService<OutboxProcessor>();
        }

        // ========================================================================
        // 6. BACKGROUND JOBS (retry unreserve / fulfill)
        // ========================================================================
        private static void AddBackgroundJobs(IServiceCollection services)
        {
            services.AddHostedService<CancelPendingRetryJob>();
            services.AddHostedService<FulfillRetryJob>();
        }
    }
}
