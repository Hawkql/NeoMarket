using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Buyers;
using B2C.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2C.Infrastructure.Persistence.Configurations
{
    public sealed class ProductSubscriptionConfiguration : IEntityTypeConfiguration<ProductSubscription>
    {
        public void Configure(EntityTypeBuilder<ProductSubscription> builder)
        {
            builder.ToTable("product_subscriptions");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(s => s.BuyerId).HasColumnName("buyer_id").IsRequired();
            builder.Property(s => s.ProductId).HasColumnName("product_id").IsRequired();

            // NotifyOn — Flags-enum, хранится как int (битмаска: InStock=1, PriceDrop=2).
            // int компактнее строки и естественно поддерживает побитовые операции.
            builder.Property(s => s.NotifyOn)
                .HasColumnName("notify_on")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(s => s.DomainEvents);

            // Уникальность пары (buyer, product) — одна подписка на товар.
            builder.HasIndex(s => new { s.BuyerId, s.ProductId })
                .IsUnique()
                .HasDatabaseName("ux_subscriptions_buyer_product");

            builder.HasIndex(s => s.ProductId)
                .HasDatabaseName("ix_subscriptions_product");

            builder.HasOne<Buyer>()
                .WithMany()
                .HasForeignKey(s => s.BuyerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
