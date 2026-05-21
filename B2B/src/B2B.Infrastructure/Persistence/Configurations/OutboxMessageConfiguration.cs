using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations
{
    public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("outbox_messages");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(m => m.EventType)
                .HasColumnName("event_type")
                .IsRequired()
                .HasMaxLength(100);

            // JSON может быть большим — text без ограничения длины
            builder.Property(m => m.Payload)
                .HasColumnName("payload")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(m => m.Destination)
                .HasColumnName("destination")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(m => m.AggregateId)
                .HasColumnName("aggregate_id");

            builder.Property(m => m.AggregateType)
                .HasColumnName("aggregate_type")
                .HasMaxLength(50);

            builder.Property(m => m.OccurredOnUtc)
                .HasColumnName("occurred_on_utc")
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.Property(m => m.ProcessedOnUtc)
                .HasColumnName("processed_on_utc")
                .HasColumnType("timestamptz");

            builder.Property(m => m.Error)
                .HasColumnName("error")
                .HasColumnType("text");

            builder.Property(m => m.RetryCount)
                .HasColumnName("retry_count")
                .HasDefaultValue(0)
                .IsRequired();

            // ====================================================================
            // ИНДЕКСЫ
            // ====================================================================

            
            builder.HasIndex(m => m.OccurredOnUtc)
                .HasDatabaseName("ix_outbox_unprocessed")
                .HasFilter("processed_on_utc IS NULL");

            // Для отладки: найти все события по агрегату
            builder.HasIndex(m => new { m.AggregateType, m.AggregateId })
                .HasDatabaseName("ix_outbox_aggregate");
        }
    }
}
