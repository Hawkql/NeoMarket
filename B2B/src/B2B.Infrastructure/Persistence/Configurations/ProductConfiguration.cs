// B2B.Infrastructure/Persistence/Configurations/ProductConfiguration.cs
using B2B.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations;

internal class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("product");
        builder.HasKey(p => p.Id);


        //говорим что мы сами генерируем guid, чтобы не пытался
        builder.Property(p=>p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // ОСНОВНЫЕ ПОЛЯ

        builder.Property(p => p.SellerId)
            .HasColumnName("seller_id")
            .IsRequired();
        builder.Property(p=>p.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(p => p.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .IsRequired()
            .HasMaxLength(5000);
        builder.Property(p => p.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(300);
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p=>p.Deleted)
            .HasColumnName("deleted")
            .IsRequired().
            HasDefaultValue(false);

        builder.Property(p => p.ModerationRound)
            .HasColumnName("moderation_round")
            .IsRequired()
            .HasDefaultValue(0);
        builder.Ignore(p => p.Blocked);

        // AUDIT (CreatedAt / UpdatedAt из IAuditableEntity)

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Ignore(p => p.DomainEvents);

        // BlockingReason — Value Object с тремя полями. Колонки добавляются
        // прямо в таблицу products: blocking_reason_reason_id (3 колонки),
        // _title, _comment. Они nullable — если товар не заблокирован, NULL.
        builder.OwnsOne(p => p.BlockingReason, br =>
        {
            br.Property(x => x.ReasonId)
                .HasColumnName("blocking_reason_reason_id");

            br.Property(x => x.Title)
                .HasColumnName("blocking_reason_title")
                .HasMaxLength(255);

            br.Property(x => x.Comment)
                .HasColumnName("blocking_reason_comment")
                .HasMaxLength(2000);
        });

        // ProductCharacteristic — Value Object без Id. Хранится в отдельной
        // таблице product_characteristics, но нет навигации, нет DbSet.
        // EF создаёт shadow-key (composite: product_id + auto int).
        builder.OwnsMany(p => p.Characteristics, c =>
        {
            c.ToTable("product_characteristics");

            c.WithOwner().HasForeignKey("product_id");

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
        builder.Navigation(p => p.Characteristics)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // FieldReport имеет свой Guid Id и должен сохраняться полноценной
        // строкой в отдельной таблице. Это HasMany, не OwnsMany.
        builder.HasMany(p => p.FieldReports)
            .WithOne()
            .HasForeignKey(fr => fr.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.FieldReports)
            .UsePropertyAccessMode(PropertyAccessMode.Field);



        // GET /api/v1/products (B2B-11) — фильтр по seller_id + status:
        builder.HasIndex(p => new { p.SellerId, p.Status })
            .HasDatabaseName("ix_products_seller_status");

        // GET /api/v1/products (B2B-7, B2C catalog) — фильтр по status + deleted:
        builder.HasIndex(p => new { p.Status, p.Deleted })
            .HasDatabaseName("ix_products_status_deleted");

        // GET /api/v1/products?category_id=... — фильтр по категории:
        builder.HasIndex(p => p.CategoryId)
            .HasDatabaseName("ix_products_category");



        // CHECK CONSTRAINTS (защита на уровне БД)
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_products_moderation_round_non_negative",
                "moderation_round >= 0");
        });
    }
}
