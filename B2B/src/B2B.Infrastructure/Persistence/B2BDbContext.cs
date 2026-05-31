using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Categories;
using B2B.Domain.Common;
using B2B.Domain.Images;
using B2B.Domain.Inventory;
using B2B.Domain.Invoices;
using B2B.Domain.Products;
using B2B.Domain.Sellers;
using B2B.Domain.Skus;
using B2B.Infrastructure.Inbox;
using B2B.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence
{
    public sealed class B2BDbContext : DbContext
    {
        private readonly IIntegrationEventMapper _eventMapper;
        private readonly IDateTimeProvider _dateTimeProvider;

        public B2BDbContext(
            DbContextOptions<B2BDbContext> options,
            IIntegrationEventMapper eventMapper,
            IDateTimeProvider dateTimeProvider)
            : base(options)
        {
            _eventMapper = eventMapper;
            _dateTimeProvider = dateTimeProvider;
        }

        // ========================================================================
        // DBSETS — только Aggregate Roots
        // ========================================================================
        public DbSet<Seller> Sellers => Set<Seller>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Sku> Skus => Set<Sku>();
        public DbSet<Image> Images => Set<Image>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
        // Инфраструктурные таблицы — отдельно от Domain
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        // Internal entities (FieldReport, InvoiceItem) НЕ публикуются как DbSet.
        // Доступ к ним — только через корни (Product.FieldReports, Invoice.Items).
        // Это поддерживает Aggregate boundary на уровне кода.

        // ========================================================================
        // CONFIGURATION SWEEP
        // ========================================================================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Подхватываем все IEntityTypeConfiguration<T> из этой сборки
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(B2BDbContext).Assembly);
        }

        // ========================================================================
        // SAVECHANGESASYNC — сердце Transactional Outbox
        // ========================================================================

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            // 1. Аудит: CreatedAt / UpdatedAt
            ApplyAuditTimestamps();

            // 2. Сбор Domain Events → Outbox
            WriteDomainEventsToOutbox();

            // 3. Атомарный SaveChanges
            return await base.SaveChangesAsync(ct);
        }

        // ========================================================================
        // ВНУТРЕННИЕ ХЕЛПЕРЫ
        // ========================================================================

        /// <summary>
        /// Сканирует ChangeTracker, проставляет CreatedAt/UpdatedAt 
        /// для всех IAuditableEntity.
        /// </summary>
        private void ApplyAuditTimestamps()
        {
            var now = _dateTimeProvider.UtcNow;

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
                        // CreatedAt не трогаем — иначе при каждом UPDATE он бы сбивался.
                        // EF сам отметит это поле как unchanged.
                        entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Сканирует все Aggregate Roots, собирает их Domain Events,
        /// маппит в Integration Events, сериализует в OutboxMessage 
        /// и добавляет в DbContext (станут частью транзакции).
        /// </summary>
        private void WriteDomainEventsToOutbox()
        {
            // Находим ВСЕ агрегаты с непустыми DomainEvents.
            // ChangeTracker.Entries() возвращает entries в неизменяемом snapshot —
            // мы можем добавлять новые сущности (OutboxMessage) внутри цикла.
            var aggregatesWithEvents = ChangeTracker.Entries()
                .Select(e => e.Entity)
                .OfType<AggregateRoot<Guid>>()
                .Where(a => a.DomainEvents.Any())
                .ToList();

            foreach (var aggregate in aggregatesWithEvents)
            {
                // Снимок событий до очистки
                var domainEvents = aggregate.DomainEvents.ToList();

                foreach (var domainEvent in domainEvents)
                {
                    // Маппер может вернуть 0, 1 или N Integration Events
                    var integrationEvents = _eventMapper.Map(domainEvent);

                    foreach (var mapped in integrationEvents)
                    {
                        var outboxMessage = new OutboxMessage
                        {
                            Id = Guid.NewGuid(),
                            EventType = mapped.EventType,
                            Destination = mapped.Destination,
                            Payload = JsonSerializer.Serialize(
                                mapped.Event,
                                mapped.Event.GetType(),
                                JsonSerializerOptions),
                            AggregateId = aggregate.Id,
                            AggregateType = aggregate.GetType().Name,
                            OccurredOnUtc = domainEvent.OccurredOnUtc,
                            RetryCount = 0
                            // ProcessedOnUtc и Error остаются null/default
                        };

                        OutboxMessages.Add(outboxMessage);
                    }
                }

                // Очищаем DomainEvents у агрегата — они уже сериализованы в Outbox.
                // Без этого при повторном SaveChanges те же события записались бы повторно.
                aggregate.ClearDomainEvents();
            }
        }

        /// <summary>
        /// Опции сериализации для Outbox payload:
        /// - snake_case поля (контракт API)
        /// - игнорирование null значений
        /// - индентация выключена (короче в БД)
        /// </summary>
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }
}
