using Amazon.S3;
using B2B.Application.Common;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Categories;
using B2B.Domain.Images;
using B2B.Domain.Inventory;
using B2B.Domain.Invoices;
using B2B.Domain.Products;
using B2B.Domain.Sellers;
using B2B.Domain.Skus;
using B2B.Infrastructure.FileStorage;
using B2B.Infrastructure.ImageProcessing;
using B2B.Infrastructure.Outbox;
using B2B.Infrastructure.Outbox.Dispatchers;
using B2B.Infrastructure.Persistence;
using B2B.Infrastructure.Persistence.Repositories;
using B2B.Infrastructure.Persistence.Services;
using B2B.Infrastructure.Security;
using Humanizer.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
namespace B2B.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration config)
        {
            AddPersistence(services, config);
            AddRepositories(services);
            AddDomainServices(services);
            AddOutbox(services, config);
            AddFileStorage(services, config);
            AddCommon(services);
            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            services.AddSingleton<ITokenService, TokenService>();
            services.Configure<JwtSettings>(config.GetSection("Jwt"));
            return services;
        }

        // ========================================================================
        // 1. PERSISTENCE (DbContext + UnitOfWork)
        // ========================================================================
        private static void AddPersistence(IServiceCollection services, IConfiguration config)
        {
            var connectionString = config.GetConnectionString("B2BDatabase")
                ?? throw new InvalidOperationException(
                    "Connection string 'B2BDatabase' not configured");

            services.AddDbContext<B2BDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(B2BDbContext).Assembly.FullName);
                });
            });
            services.AddScoped<B2B.Application.Common.Interface.ITransactionManager, TransactionManager>();
            services.AddScoped<IIdempotencyStore, IdempotencyStore>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
           
        }

        // ========================================================================
        // 2. REPOSITORIES (все 7)
        // ========================================================================
        private static void AddRepositories(IServiceCollection services)
        {
            services.AddScoped<IInventoryReservationRepository, InventoryReservationRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ISkuRepository, SkuRepository>();
            services.AddScoped<IImageRepository, ImageRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<ISellerRepository, SellerRepository>();     
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>(); 
        }

        // ========================================================================
        // 3. DOMAIN SERVICES (реализации интерфейсов из Domain)
        // ========================================================================
        private static void AddDomainServices(IServiceCollection services)
        {
            services.AddScoped<ICategoryHierarchyValidator, CategoryHierarchyValidator>();
        }

        // ========================================================================
        // 4. OUTBOX (Mapper + Dispatchers + BackgroundService)
        // ========================================================================
        private static void AddOutbox(IServiceCollection services, IConfiguration config)
        {
            // Mapper Domain Event → Integration Event
            services.AddScoped<IIntegrationEventMapper, IntegrationEventMapper>();

            // Опции клиентов
            services.Configure<ModerationClientOptions>(config.GetSection("Moderation"));
            services.Configure<B2cClientOptions>(config.GetSection("B2c"));

            // HTTP клиенты для диспетчеров через IHttpClientFactory
            services.AddHttpClient<HttpModerationDispatcher>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<ModerationClientOptions>>().Value;
                http.BaseAddress = new Uri(options.BaseUrl);
                http.Timeout = TimeSpan.FromSeconds(10);
            });

            services.AddHttpClient<HttpB2cDispatcher>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<B2cClientOptions>>().Value;
                http.BaseAddress = new Uri(options.BaseUrl);
                http.Timeout = TimeSpan.FromSeconds(10);
            });

            // Регистрируем оба под общим интерфейсом — DispatcherRegistry получит обоих
            services.AddScoped<IIntegrationEventDispatcher>(sp =>
                sp.GetRequiredService<HttpModerationDispatcher>());
            services.AddScoped<IIntegrationEventDispatcher>(sp =>
                sp.GetRequiredService<HttpB2cDispatcher>());

            services.AddScoped<DispatcherRegistry>();

            // BackgroundService — Singleton, scope для DbContext создаёт сам внутри
            services.AddHostedService<OutboxProcessor>();
        }

        // ========================================================================
        // 5. FILE STORAGE (MinIO + ImageValidator)
        // ========================================================================
        private static void AddFileStorage(IServiceCollection services, IConfiguration config)
        {
            services.Configure<MinioOptions>(config.GetSection("Minio"));

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
                var s3Config = new AmazonS3Config
                {
                    ServiceURL = options.ServiceUrl,
                    ForcePathStyle = true,
                    UseHttp = options.ServiceUrl.StartsWith("http://")
                };
                return new AmazonS3Client(options.AccessKey, options.SecretKey, s3Config);
            });

            services.AddScoped<IFileStorage, MinioFileStorage>();
            services.AddScoped<IImageValidator, ImageSharpValidator>();
        }

        // ========================================================================
        // 6. COMMON (DateTimeProvider)
        // ========================================================================
        private static void AddCommon(IServiceCollection services)
        {
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        }
    }
}
