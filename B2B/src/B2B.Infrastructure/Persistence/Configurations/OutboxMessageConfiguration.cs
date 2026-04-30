using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations
{
    internal class OutboxMessageConfiguration: IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("outbox_messages");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Type)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(m => m.Payload)
                .HasColumnType("jsonb")    
                .IsRequired();

            builder.Property(m => m.OccurredOnUtc).IsRequired();
            builder.Property(m => m.ProcessedOnUtc);
            builder.Property(m => m.Error).HasMaxLength(2000);
            builder.Property(m => m.RetryCount).IsRequired();

            // Индекс для быстрого поиска необработанных
            builder.HasIndex(m => new { m.ProcessedOnUtc, m.OccurredOnUtc })
                .HasDatabaseName("ix_outbox_unprocessed");
        }
    }
}
