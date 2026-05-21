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

            builder.Property(s=>s.Id)
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

            // Один основной URL по спеке (обратная совместимость).
            // Дополнительные картинки SKU — в polymorphic таблице images.
            builder.Property(s => s.ImageUrl)
                .HasColumnName("image")
                .IsRequired()
                .HasMaxLength(2000);  // S3 URL может быть длинным

            // ====================================================================
            // 4. ФИНАНСОВЫЕ ПОЛЯ (kopecks, integer)
            // ====================================================================
            // ВАЖНО: int даёт лимит ~2.1B копеек = ~21M ₽. Достаточно для большинства
            // товаров. Если понадобится больше — миграция до bigint безопасна.
            builder.Property(s => s.Price)
                .HasColumnName("price")
                .IsRequired();

            builder.Property(s => s.CostPrice)
                .HasColumnName("cost_price")
                .IsRequired();

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

                // Shadow ID — auto-increment
                c.Property<int>("id");
                c.HasKey("sku_id", "id");

                c.Property(x => x.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(100);

                c.Property(x => x.Value)
                    .HasColumnName("value")
                    .IsRequired()
                    .HasMaxLength(500);
            });

            // Backing field _characteristics
            builder.Navigation(s => s.Characteristics)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // ====================================================================
            // 10. ИНДЕКСЫ
            // ====================================================================
            // Запрос "все SKU товара" (ISkuRepository.GetByProductIdAsync)
            builder.HasIndex(s => s.ProductId)
                .HasDatabaseName("ix_skus_product");

            // Partial index — запрос "не удалённые SKU товара" (для каталога):
            // CREATE INDEX ... WHERE deleted = false;
            // Postgres-specific: индексирует только активные строки, экономит место
            // и ускоряет запросы из B2C.
            builder.HasIndex(s => s.ProductId)
                .HasDatabaseName("ix_skus_product_active")
                .HasFilter("deleted = false");

            // ====================================================================
            // 11. CHECK CONSTRAINTS (защита инвариантов на уровне БД)
            // ====================================================================
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "ck_skus_price_positive",
                    "price > 0");

                t.HasCheckConstraint(
                    "ck_skus_cost_price_positive",
                    "cost_price > 0");

                t.HasCheckConstraint(
                    "ck_skus_discount_valid",
                    "discount >= 0 AND discount < price");

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
