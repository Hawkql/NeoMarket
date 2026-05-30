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
    public sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
    {
        public void Configure(EntityTypeBuilder<Collection> builder)
        {
            builder.ToTable("collections");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(c => c.Slug).HasColumnName("slug").IsRequired().HasMaxLength(100);
            builder.Property(c => c.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
            builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(2000);
            builder.Property(c => c.CoverImageUrl).HasColumnName("cover_image_url").HasMaxLength(2000);
            builder.Property(c => c.Priority).HasColumnName("priority").IsRequired().HasDefaultValue(0);
            builder.Property(c => c.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

            builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(c => c.DomainEvents);

            // ProductIds — IReadOnlyList<Guid> с backing field _productIds.
            // Маппим как PostgreSQL uuid[] (Npgsql нативно). Доступ через поле,
            // потому что публичное свойство readonly (нет сеттера).
            //
            // Почему массив, а не отдельная таблица collection_products:
            //   - порядок значим и хранится естественно в массиве;
            //   - подборка читается целиком (не нужны частичные запросы по товарам);
            //   - проще, чем join-таблица для read-mostly данных.
            builder.Property<List<Guid>>("_productIds")
                .HasColumnName("product_ids")
                .HasColumnType("uuid[]")
                .HasField("_productIds")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .IsRequired();

            // Игнорируем публичное readonly-свойство ProductIds — EF работает через _productIds.
            builder.Ignore(c => c.ProductIds);

            builder.HasIndex(c => c.Slug)
                .IsUnique()
                .HasDatabaseName("ux_collections_slug");

            builder.HasIndex(c => new { c.IsActive, c.Priority })
                .HasDatabaseName("ix_collections_active_priority");
        }
    }
}
