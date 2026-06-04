using B2C.Domain.Buyers;
using B2C.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2C.Infrastructure.Persistence.Configurations
{
    public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("orders");
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(o => o.BuyerId).HasColumnName("buyer_id").IsRequired();

            builder.Property(o => o.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasConversion(
                    vo => vo.Value,
                    guid => IdempotencyKey.From(guid))
                .IsRequired();

            // ====== Snapshot адреса — owned type (6 колонок) ======
            builder.OwnsOne(o => o.Address, addr =>
            {
                addr.Property(a => a.OriginalAddressId).HasColumnName("address_original_id");
                addr.Property(a => a.Country).HasColumnName("address_country").HasMaxLength(100).IsRequired();
                addr.Property(a => a.City).HasColumnName("address_city").HasMaxLength(100).IsRequired();
                addr.Property(a => a.Street).HasColumnName("address_street").HasMaxLength(200).IsRequired();
                addr.Property(a => a.House).HasColumnName("address_house").HasMaxLength(20);
                addr.Property(a => a.Apartment).HasColumnName("address_apartment").HasMaxLength(20);
                addr.Property(a => a.PostalCode).HasColumnName("address_postal_code").HasMaxLength(20);
            });
            builder.Navigation(o => o.Address).IsRequired();

            // ====== Payment ======
            builder.Property(o => o.PaymentMethodId).HasColumnName("payment_method_id").IsRequired();
            builder.Property(o => o.PaymentMethodType)
                .HasColumnName("payment_method_type")
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("CARD");

            // ====== Status enum как string ======
            builder.Property(o => o.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // ====== Money ======
            builder.Property(o => o.Subtotal).HasColumnName("subtotal").IsRequired();
            builder.Property(o => o.DeliveryCost)
                .HasColumnName("delivery_cost")
                .IsRequired()
                .HasDefaultValue(0);
            builder.Ignore(o => o.Total);  // computed property

            // ====== Optional fields ======
            builder.Property(o => o.Comment).HasColumnName("comment").HasMaxLength(1000);
            builder.Property(o => o.CancelReason).HasColumnName("cancel_reason").HasMaxLength(500);
            builder.Property(o => o.PaidAt).HasColumnName("paid_at").HasColumnType("timestamptz");
            builder.Property(o => o.DeliveredAt).HasColumnName("delivered_at").HasColumnType("timestamptz");

            // ====== Async retry tracking ======
            builder.Property(o => o.LastUnreserveAttemptAt)
                .HasColumnName("last_unreserve_attempt_at").HasColumnType("timestamptz");
            builder.Property(o => o.FulfillCompletedAt)
                .HasColumnName("fulfill_completed_at").HasColumnType("timestamptz");

            // ====== Audit ======
            builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(o => o.DomainEvents);

            // ====== OrderItem — OwnsMany ======
            builder.OwnsMany(o => o.Items, item =>
            {
                item.ToTable("order_items");
                item.WithOwner().HasForeignKey("order_id");

                item.HasKey(i => i.Id);
                item.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();
                item.Property(i => i.OrderId).HasColumnName("order_id");
                item.Property(i => i.SkuId).HasColumnName("sku_id").IsRequired();
                item.Property(i => i.ProductId).HasColumnName("product_id").IsRequired();
                item.Property(i => i.ProductTitle).HasColumnName("product_title").IsRequired().HasMaxLength(500);
                item.Property(i => i.SkuName).HasColumnName("sku_name").IsRequired().HasMaxLength(255);
                item.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();
                item.Property(i => i.UnitPrice).HasColumnName("unit_price").IsRequired();

                item.Ignore(i => i.LineTotal);

                item.HasIndex(i => i.SkuId).HasDatabaseName("ix_order_items_sku");
            });

            builder.Navigation(o => o.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // ====== Indexes ======
            builder.HasIndex(o => new { o.BuyerId, o.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_orders_buyer_idempotency");

            builder.HasIndex(o => new { o.BuyerId, o.CreatedAt })
                .HasDatabaseName("ix_orders_buyer_created");

            builder.HasIndex(o => o.Status)
                .HasDatabaseName("ix_orders_status");

            builder.HasOne<Buyer>()
                .WithMany()
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(t =>
                t.HasCheckConstraint("ck_orders_subtotal_non_negative", "subtotal >= 0"));
        }
    }
}