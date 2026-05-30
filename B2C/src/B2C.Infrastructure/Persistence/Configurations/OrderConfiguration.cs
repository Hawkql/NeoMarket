using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            // ================================================================
            // IdempotencyKey — Value Object (обёртка над Guid).
            // Один скалярный value → используем HasConversion (value-to-value),
            // а не OwnsOne (OwnsOne создал бы вложенную таблицу/owned-сущность, что
            // избыточно для single-value VO). Конвертим VO ↔ Guid.
            // ================================================================
            builder.Property(o => o.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasConversion(
                    vo => vo.Value,                     // VO → Guid (в БД)
                    guid => IdempotencyKey.From(guid))  // Guid → VO (из БД)
                .IsRequired();

            // ================================================================
            // DeliveryAddress — Value Object (обёртка над string).
            // Тоже single-value → HasConversion.
            // ================================================================
            builder.Property(o => o.DeliveryAddress)
                .HasColumnName("delivery_address")
                .HasConversion(
                    vo => vo.Value,
                    str => DeliveryAddress.Of(str))
                .HasMaxLength(500)
                .IsRequired();

            // ================================================================
            // OrderStatus — status-enum, string по B2B-стилю.
            // ================================================================
            builder.Property(o => o.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(o => o.TotalAmount)
                .HasColumnName("total_amount")
                .IsRequired();   // копейки, int

            // Отслеживание async-операций.
            builder.Property(o => o.LastUnreserveAttemptAt)
                .HasColumnName("last_unreserve_attempt_at")
                .HasColumnType("timestamptz");

            builder.Property(o => o.FulfillCompletedAt)
                .HasColumnName("fulfill_completed_at")
                .HasColumnType("timestamptz");

            builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(o => o.DomainEvents);

            // ================================================================
            // OrderItem — внутренняя entity, OwnsMany. Снимки цен (US-ORD-02).
            // ================================================================
            builder.OwnsMany(o => o.Items, item =>
            {
                item.ToTable("order_items");

                item.WithOwner().HasForeignKey("order_id");

                item.HasKey(i => i.Id);
                item.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();
                item.Property(i => i.OrderId).HasColumnName("order_id");
                item.Property(i => i.SkuId).HasColumnName("sku_id").IsRequired();
                item.Property(i => i.ProductId).HasColumnName("product_id").IsRequired();

                // Snapshot-поля — фиксируются при создании, не меняются.
                item.Property(i => i.ProductTitle).HasColumnName("product_title").IsRequired().HasMaxLength(500);
                item.Property(i => i.SkuName).HasColumnName("sku_name").IsRequired().HasMaxLength(255);
                item.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();
                item.Property(i => i.UnitPrice).HasColumnName("unit_price").IsRequired();  // копейки

                // LineTotal — вычисляемое свойство в Domain, в БД не храним.
                item.Ignore(i => i.LineTotal);

                item.HasIndex(i => i.SkuId).HasDatabaseName("ix_order_items_sku");
            });

            builder.Navigation(o => o.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // ================================================================
            // ИНДЕКСЫ
            // ================================================================

            // Idempotency US-ORD-01: один (buyer, idempotency_key) = один заказ.
            // Это БД-гарантия против двойного checkout (race между двумя submit).
            builder.HasIndex(o => new { o.BuyerId, o.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_orders_buyer_idempotency");

            // Список заказов покупателя по дате (ListMyOrders).
            builder.HasIndex(o => new { o.BuyerId, o.CreatedAt })
                .HasDatabaseName("ix_orders_buyer_created");

            // Background-job: заказы в CANCEL_PENDING / DELERED+не-fulfilled.
            // Индекс по статусу для ListPendingCancelOlderThanAsync / ListPendingFulfillAsync.
            builder.HasIndex(o => o.Status)
                .HasDatabaseName("ix_orders_status");

            builder.HasOne<Buyer>()
                .WithMany()
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);  // нельзя удалить покупателя с заказами

            // CHECK: TotalAmount >= 0.
            builder.ToTable(t =>
                t.HasCheckConstraint("ck_orders_total_non_negative", "total_amount >= 0"));
        }
    }
}
