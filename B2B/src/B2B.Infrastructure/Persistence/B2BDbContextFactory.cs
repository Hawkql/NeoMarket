using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace B2B.Infrastructure.Persistence
{
    public sealed class B2BDbContextFactory : IDesignTimeDbContextFactory<B2BDbContext>
    {
        public B2BDbContext CreateDbContext(string[] args)
        {
            // Ищем appsettings.json в проекте API (typical setup)
            var basePath = Path.Combine(
                Directory.GetCurrentDirectory(), "..", "B2B.Api");

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("B2BDatabase")
                ?? throw new InvalidOperationException(
                    "Connection string 'B2BDatabase' not found");

            var options = new DbContextOptionsBuilder<B2BDbContext>()
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(B2BDbContext).Assembly.FullName);
                })
                .Options;

            // Заглушки — при миграциях SaveChanges не вызывается, эти зависимости не используются
            return new B2BDbContext(
                options,
                new NoOpEventMapper(),
                new NoOpDateTimeProvider());
        }

        private sealed class NoOpEventMapper : IIntegrationEventMapper
        {
            public IEnumerable<MappedIntegrationEvent> Map(Domain.Common.DomainEvent _)
                => Enumerable.Empty<MappedIntegrationEvent>();
        }

        private sealed class NoOpDateTimeProvider : IDateTimeProvider
        {
            public DateTime UtcNow => DateTime.UtcNow;
        }
    }
}
