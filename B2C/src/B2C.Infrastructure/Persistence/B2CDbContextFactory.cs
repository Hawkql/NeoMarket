using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace B2C.Infrastructure.Persistence
{
    public sealed class B2CDbContextFactory : IDesignTimeDbContextFactory<B2CDbContext>
    {
        public B2CDbContext CreateDbContext(string[] args)
        {
            var connection = Environment.GetEnvironmentVariable("B2C_DESIGN_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=b2c;Username=postgres;Password=postgres";

            var options = new DbContextOptionsBuilder<B2CDbContext>()
                .UseNpgsql(connection)
                .Options;

            return new B2CDbContext(options, new NoopEventMapper(), new SystemClock());
        }

        // No-op заглушки только для design-time.
        private sealed class NoopEventMapper : IIntegrationEventMapper
        {
            public System.Collections.Generic.IEnumerable<MappedIntegrationEvent> Map(DomainEvent e)
                => System.Array.Empty<MappedIntegrationEvent>();
        }

        private sealed class SystemClock : IDateTimeProvider
        {
            public DateTime UtcNow => DateTime.UtcNow;
        }
    }
}
