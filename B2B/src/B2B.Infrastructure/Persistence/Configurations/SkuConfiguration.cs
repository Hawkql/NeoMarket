using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Skus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations
{
    public sealed class SkuConfiguration : IEntityTypeConfiguration<Sku>
    {
        public void Configure(EntityTypeBuilder<Sku> builder)
        {
            builder.ToTable("skus");

            builder.HasKey(x => x.Id);

            builder.Property(s => s.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(s => s.ProductId)
                .HasColumnName("product_id")
                .IsRequired();

            // ====================================================================
            // 3. КОНТЕНТ
            // ====================================================================
            builder.Property(s => s.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            // Cover-картинка SKU. Опциональна (галерея — в polymorphic images).
            builder.Property(s => s.ImageUrl)
                .HasColumnName("image")
                .HasMaxLength(2000);
            // НЕ IsRequired — по спеке images у SKU опциональны

            // Артикул продавца. Опционален.
            builder.Property(s => s.Article)
                .HasColumnName("article")
                .HasMaxLength(255);
            // НЕ IsRequired — nullable по спеке

            // ====================================================================
            // 4. ФИНАНСОВЫЕ ПОЛЯ (kopecks, integer)
            // ====================================================================
            builder.Property(s => s.Price)
                .HasColumnName("price")
                .IsRequired();

            // cost_price nullable по спеке (видна только seller, может отсутствовать)
            builder.Property(s => s.CostPrice)
                .HasColumnName("cost_price");
            // НЕ IsRequired

            builder.Property(s => s.Discount)
                .HasColumnName("discount")
                .IsRequired()
                .HasDefaultValue(0);

            // ====================================================================
            // 5. INVENTORY ПОЛЯ
            // ====================================================================
            builder.Property(s => s.ActiveQuantity)
                .HasColumnName("active_quantity")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(s => s.ReservedQuantity)
                .HasColumnName("reserved_quantity")
                .IsRequired()
                .HasDefaultValue(0);

            // ====================================================================
            // 6. SOFT DELETE
            // ====================================================================
            builder.Property(s => s.Deleted)
                .HasColumnName("deleted")
                .IsRequired()
                .HasDefaultValue(false);

            // ====================================================================
            // 7. AUDIT
            // ====================================================================
            builder.Property(s => s.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.Property(s => s.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamptz")
                .IsRequired();

            // ====================================================================
            // 8. DOMAIN EVENTS - игнорируем
            // ====================================================================
            builder.Ignore(s => s.DomainEvents);

            // ====================================================================
            // 9. OWNED COLLECTION: SkuCharacteristic
            // ====================================================================
            builder.OwnsMany(s => s.Characteristics, c =>
            {
                c.ToTable("sku_characteristics");

                c.WithOwner().HasForeignKey("sku_id");

                // реальный Guid Id вместо shadow int
                c.HasKey(x => x.Id);
                c.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                c.Property(x => x.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(100);

                c.Property(x => x.Value)
                    .HasColumnName("value")
                    .IsRequired()
                    .HasMaxLength(500);
            });

            builder.Navigation(s => s.Characteristics)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // ====================================================================
            // 10. ИНДЕКСЫ
            // ====================================================================
            builder.HasIndex(s => s.ProductId)
                .HasDatabaseName("ix_skus_product");

            builder.HasIndex(s => s.ProductId)
                .HasDatabaseName("ix_skus_product_active")
                .HasFilter("deleted = false");

            // ====================================================================
            // 11. CHECK CONSTRAINTS (приведены к новым правилам Domain)
            // ====================================================================
            builder.ToTable(t =>
            {
                // price >= 0 (спека minimum: 0)
                t.HasCheckConstraint(
                    "ck_skus_price_non_negative",
                    "price >= 0");

                // cost_price: либо NULL, либо >= 0
                t.HasCheckConstraint(
                    "ck_skus_cost_price_valid",
                    "cost_price IS NULL OR cost_price >= 0");

                // discount >= 0, и меньше price только когда price > 0
                t.HasCheckConstraint(
                    "ck_skus_discount_valid",
                    "discount >= 0 AND (price = 0 OR discount < price)");

                t.HasCheckConstraint(
                    "ck_skus_active_quantity_non_negative",
                    "active_quantity >= 0");

                t.HasCheckConstraint(
                    "ck_skus_reserved_quantity_non_negative",
                    "reserved_quantity >= 0");
            });
        }
    }
}
