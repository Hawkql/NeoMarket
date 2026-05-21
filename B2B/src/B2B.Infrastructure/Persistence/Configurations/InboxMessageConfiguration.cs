using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations
{
    public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
    {
        public void Configure(EntityTypeBuilder<InboxMessage> builder)
        {
            builder.ToTable("inbox_messages");

            // PRIMARY KEY — IdempotencyKey (а не отдельное Id поле).
            // Это критично: при дублирующемся событии INSERT упадёт сразу,
            // не дав обработать payload дважды.
            builder.HasKey(m => m.IdempotencyKey);

            builder.Property(m => m.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .ValueGeneratedNever();

            builder.Property(m => m.MessageType)
                .HasColumnName("message_type")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(m => m.Source)
                .HasColumnName("source")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(m => m.Payload)
                .HasColumnName("payload")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(m => m.ReceivedOnUtc)
                .HasColumnName("received_on_utc")
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.Property(m => m.ProcessedOnUtc)
                .HasColumnName("processed_on_utc")
                .HasColumnType("timestamptz");

            builder.Property(m => m.Error)
                .HasColumnName("error")
                .HasColumnType("text");

            // Партиал индекс по unprocessed — для retry-логики если она понадобится
            builder.HasIndex(m => m.ReceivedOnUtc)
                .HasDatabaseName("ix_inbox_unprocessed")
                .HasFilter("processed_on_utc IS NULL");
        }
    }
}
