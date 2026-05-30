using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Addresses;
using B2C.Domain.Buyers;
using B2C.Domain.Carts;
using B2C.Domain.Common;
using B2C.Domain.Favorites;
using B2C.Domain.HomePage;
using B2C.Domain.Orders;
using B2C.Domain.Subscriptions;
using B2C.Infrastructure.Inbox;
using B2C.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;


namespace B2C.Infrastructure.Persistence
{
    public sealed class B2CDbContext : DbContext
    {
        private readonly IIntegrationEventMapper _eventMapper;
        private readonly IDateTimeProvider _clock;

        public B2CDbContext(
            DbContextOptions<B2CDbContext> options,
            IIntegrationEventMapper eventMapper,
            IDateTimeProvider clock)
            : base(options)
        {
            _eventMapper = eventMapper;
            _clock = clock;
        }

        // DBSETS — только Aggregate Roots
        public DbSet<Buyer> Buyers => Set<Buyer>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<ProductSubscription> Subscriptions => Set<ProductSubscription>();
        public DbSet<Banner> Banners => Set<Banner>();
        public DbSet<BannerEvent> BannerEvents => Set<BannerEvent>();
        public DbSet<Collection> Collections => Set<Collection>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<Order> Orders => Set<Order>();

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(B2CDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            ApplyAuditTimestamps();
            WriteDomainEventsToOutbox();  // внешняя доставка (на будущее, сейчас пусто)
            return await base.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Собирает domain events со всех отслеживаемых агрегатов и ОЧИЩАЕТ их.
        /// Вызывается TransactionManager'ом ДО SaveChanges, события публикуются
        /// через MediatR ПОСЛЕ успешного commit (collect-then-dispatch паттерн).
        /// 
        /// Почему не публиковать прямо в SaveChanges: если handler сработает, а
        /// транзакция откатится — получим побочные эффекты для несохранённых данных.
        /// </summary>
        public IReadOnlyList<DomainEvent> ExtractDomainEvents()
        {
            var aggregates = ChangeTracker.Entries()
                .Select(e => e.Entity)
                .OfType<AggregateRoot<Guid>>()
                .Where(a => a.DomainEvents.Any())
                .ToList();

            var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

            foreach (var aggregate in aggregates)
                aggregate.ClearDomainEvents();

            return events;
        }

        private void ApplyAuditTimestamps()
        {
            var now = _clock.UtcNow;
            foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        entry.Entity.UpdatedAt = now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Внешняя доставка: маппит domain events в Integration Events для Outbox.
        /// Сейчас для B2C маппер возвращает пусто (нет async-событий наружу).
        /// Инфраструктура оставлена на будущее (аналитика, нотификации).
        /// 
        /// ВАЖНО: этот метод НЕ очищает DomainEvents — это делает ExtractDomainEvents
        /// для внутренней доставки. Но поскольку ExtractDomainEvents вызывается ДО
        /// SaveChanges и очищает события, здесь к моменту вызова DomainEvents уже пусты.
        /// Поэтому Outbox-sweep сейчас фактически no-op. Когда понадобится внешняя
        /// доставка — порядок вызовов в TransactionManager будет пересмотрен.
        /// </summary>
        private void WriteDomainEventsToOutbox()
        {
            var aggregatesWithEvents = ChangeTracker.Entries()
                .Select(e => e.Entity)
                .OfType<AggregateRoot<Guid>>()
                .Where(a => a.DomainEvents.Any())
                .ToList();

            foreach (var aggregate in aggregatesWithEvents)
            {
                foreach (var domainEvent in aggregate.DomainEvents.ToList())
                {
                    foreach (var mapped in _eventMapper.Map(domainEvent))
                    {
                        OutboxMessages.Add(new OutboxMessage
                        {
                            Id = Guid.NewGuid(),
                            EventType = mapped.EventType,
                            Destination = mapped.Destination,
                            Payload = JsonSerializer.Serialize(mapped.Event, mapped.Event.GetType(), JsonOptions),
                            AggregateId = aggregate.Id,
                            AggregateType = aggregate.GetType().Name,
                            OccurredOnUtc = domainEvent.OccurredOnUtc,
                            RetryCount = 0,
                        });
                    }
                }
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };
    }
}

