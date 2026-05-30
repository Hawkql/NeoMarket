using B2C.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2C.Infrastructure.Persistence.Configurations
{
    public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
    {
        public void Configure(EntityTypeBuilder<InboxMessage> builder)
        {
            builder.ToTable("inbox_messages");

            // IdempotencyKey — PRIMARY KEY: дубль INSERT падает = защита от двойной обработки.
            builder.HasKey(x => x.IdempotencyKey);

            builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").ValueGeneratedNever();
            builder.Property(x => x.MessageType).HasColumnName("message_type").IsRequired().HasMaxLength(200);
            builder.Property(x => x.Source).HasColumnName("source").IsRequired().HasMaxLength(50);
            builder.Property(x => x.Payload).HasColumnName("payload").IsRequired().HasColumnType("jsonb");
            builder.Property(x => x.ReceivedOnUtc).HasColumnName("received_on_utc").HasColumnType("timestamptz").IsRequired();
            builder.Property(x => x.ProcessedOnUtc).HasColumnName("processed_on_utc").HasColumnType("timestamptz");
            builder.Property(x => x.Error).HasColumnName("error");
        }
    }
}
