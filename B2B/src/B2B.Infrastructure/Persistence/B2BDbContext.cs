using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using B2B.Domain.Common;
using B2B.Domain.Invoices;
using B2B.Domain.Products;
using B2B.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence
{
    public class B2BDbContext :DbContext
    {
        public DbSet<Product>products => Set<Product>();
        public DbSet<Invoice>invoices => Set<Invoice>();
        public DbSet<OutboxMessage> OutboxMessage => Set<OutboxMessage>();

        public B2BDbContext(DbContextOptions<B2BDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(B2BDbContext).Assembly);
        }
        public override async Task<int>SaveChangesAsync(CancellationToken ct = default)
        {
            ConvertDomainEventsToOutBoxMessages();
            return await base.SaveChangesAsync(ct);
        }
        private void ConvertDomainEventsToOutBoxMessages()
        {
            var aggregates = ChangeTracker
                .Entries<AggregateRoot<Guid>>()
                .Where(e=>e.Entity.DomainEvents.Any())
                .Select(e=>e.Entity)
                .ToList();
            var outboxMessages =aggregates
                .SelectMany(a=>a.DomainEvents.Select(de=>new OutboxMessage
                {
                    Id =  Guid .NewGuid(),
                    Type =de.GetType().AssemblyQualifiedName!,
                    OccurredOnUtc =DateTime.UtcNow,
                    Payload =JsonSerializer.Serialize(de,de.GetType()),
                })).ToList();
            foreach (var aggregate in aggregates)
                aggregate.ClearDomainEvents();
            OutboxMessage.AddRange(outboxMessages);
        }
    }
}
