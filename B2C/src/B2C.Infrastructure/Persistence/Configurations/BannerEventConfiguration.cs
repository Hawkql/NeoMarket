using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.HomePage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2C.Infrastructure.Persistence.Configurations
{
    public sealed class BannerEventConfiguration : IEntityTypeConfiguration<BannerEvent>
    {
        public void Configure(EntityTypeBuilder<BannerEvent> builder)
        {
            builder.ToTable("banner_events");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(e => e.BannerId).HasColumnName("banner_id").IsRequired();
            builder.Property(e => e.BuyerId).HasColumnName("buyer_id");        // nullable — гость
            builder.Property(e => e.SessionId).HasColumnName("session_id").HasMaxLength(128);

            // BannerEventType — простой enum (Impression/Click), храним как int.
            builder.Property(e => e.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(e => e.DomainEvents);

            // Индекс для CTR-аналитики: события по баннеру за период.
            builder.HasIndex(e => new { e.BannerId, e.OccurredAt })
                .HasDatabaseName("ix_banner_events_banner_time");

            builder.HasOne<Banner>()
                .WithMany()
                .HasForeignKey(e => e.BannerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
