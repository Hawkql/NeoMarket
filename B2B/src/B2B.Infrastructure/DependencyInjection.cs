using global::B2B.Application.common.Interface;
using global::B2B.Domain.Invoices;
using global::B2B.Domain.Products;
using global::B2B.Infrastructure.Messaging;
using global::B2B.Infrastructure.Persistence;
using global::B2B.Infrastructure.Persistence.Outbox;
using global::B2B.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace B2B.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. EF Core с PostgreSQL
            services.AddDbContext<B2BDbContext>(opt =>
                opt.UseNpgsql(
                    configuration.GetConnectionString("Postgres")
                    ?? throw new InvalidOperationException("Postgres connection string not found"),
                    npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "b2b")));


            // 2. Репозитории и UnitOfWork
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 3. Kafka producer (Singleton — переиспользуем соединение)
            services.AddSingleton<IKafkaProducer, KafkaProducer>();
            services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();

            // 4. Outbox processor (фоновая задача)
            services.AddHostedService<OutboxProcessor>();

            return services;
        }
    }
}
