using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Repository;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ─── PostgreSQL ───────────────────────────────────────────────────────
            services.AddDbContext<CatalogDbContext>(opts =>
            {
                opts.UseNpgsql(
                    configuration.GetConnectionString("Postgres"),
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
                        npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), null);
                        npgsql.CommandTimeout(30);
                    });

                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                    opts.EnableSensitiveDataLogging().EnableDetailedErrors();
            });

            // ─── Redis ────────────────────────────────────────────────────────────
            services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = configuration.GetConnectionString("Redis");
                opts.InstanceName = "b2c-catalog:";
            });

            // ─── Repositories (реализуют интерфейсы из Domain) ───────────────────
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ISkuRepository, SkuRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICategoryFilterRepository, CategoryFilterRepository>();

            return services;
        }

        public static async Task ApplyMigrationsAsync(this IServiceProvider sp)
        {
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await db.Database.MigrateAsync();
        }
    }
}
