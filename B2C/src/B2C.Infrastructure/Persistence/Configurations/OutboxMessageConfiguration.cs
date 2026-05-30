using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2C.Infrastructure.Persistence.Configurations
{
    public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(200);
            builder.Property(x => x.Payload).HasColumnName("payload").IsRequired().HasColumnType("jsonb");
            builder.Property(x => x.Destination).HasColumnName("destination").IsRequired().HasMaxLength(50);
            builder.Property(x => x.AggregateId).HasColumnName("aggregate_id");
            builder.Property(x => x.AggregateType).HasColumnName("aggregate_type").HasMaxLength(100);
            builder.Property(x => x.OccurredOnUtc).HasColumnName("occurred_on_utc").HasColumnType("timestamptz").IsRequired();
            builder.Property(x => x.ProcessedOnUtc).HasColumnName("processed_on_utc").HasColumnType("timestamptz");
            builder.Property(x => x.Error).HasColumnName("error");
            builder.Property(x => x.RetryCount).HasColumnName("retry_count").IsRequired().HasDefaultValue(0);

            // Индекс для фонового процессора: выбирать необработанные, старые сначала.
            builder.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc })
                .HasDatabaseName("ix_outbox_unprocessed")
                .HasFilter("processed_on_utc IS NULL");
        }
    }
}
