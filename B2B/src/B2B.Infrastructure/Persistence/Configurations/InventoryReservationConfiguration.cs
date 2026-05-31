using B2B.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations
{
    internal sealed class InventoryReservationConfiguration
        : IEntityTypeConfiguration<InventoryReservation>
    {
        public void Configure(EntityTypeBuilder<InventoryReservation> builder)
        {
            builder.ToTable("inventory_reservations");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id).HasColumnName("id");

            builder.Property(r => r.OrderId)
                .HasColumnName("order_id")
                .IsRequired();

            builder.Property(r => r.SkuId)
                .HasColumnName("sku_id")
                .IsRequired();

            builder.Property(r => r.Quantity)
                .HasColumnName("quantity")
                .IsRequired();

            builder.Property(r => r.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamptz")
                .IsRequired();

            // Уникальность по (order_id, sku_id) — в одном заказе один SKU не может
            // быть зарезервирован дважды (reserve агрегирует quantity до INSERT).
            builder.HasIndex(r => new { r.OrderId, r.SkuId })
                .IsUnique()
                .HasDatabaseName("ux_inventory_reservations_order_sku");

            // Для быстрого поиска по order_id при unreserve
            builder.HasIndex(r => r.OrderId)
                .HasDatabaseName("ix_inventory_reservations_order");

            builder.ToTable(t =>
                t.HasCheckConstraint(
                    "ck_inventory_reservations_quantity_positive",
                    "quantity > 0"));
        }
    }
}