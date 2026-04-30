// B2B.Infrastructure/Persistence/Configurations/ProductConfiguration.cs
using B2B.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations;

internal class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).HasMaxLength(255).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(5000);
        builder.Property(p => p.Slug).HasMaxLength(255).IsRequired();
        builder.Property(p => p.CategoryId).IsRequired();
        builder.Property(p => p.SellerId).IsRequired();
        builder.Property(p => p.Status).HasConversion<int>().IsRequired();

        builder.Ignore(p => p.DomainEvents);

        // Skus как обычная связь (Sku — это сущность, у неё свой Id и жизненный цикл)
        builder.HasMany(p => p.Skus)
            .WithOne()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Product.Skus))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Images — тоже сущность
        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Product.Images))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Characteristics — OWNED TYPE
        // Будет создана отдельная таблица "product_characteristics"
        builder.OwnsMany(p => p.Characteristics, charBuilder =>
        {
            charBuilder.ToTable("product_characteristics");
            charBuilder.WithOwner().HasForeignKey("ProductId");
            charBuilder.HasKey(c => c.Id);
            charBuilder.Property(c => c.Name).HasMaxLength(100).IsRequired();
            charBuilder.Property(c => c.Value).HasMaxLength(255).IsRequired();
        });

        builder.Metadata
            .FindNavigation(nameof(Product.Characteristics))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class SkuConfiguration : IEntityTypeConfiguration<Sku>
{
    public void Configure(EntityTypeBuilder<Sku> builder)
    {
        builder.ToTable("skus");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProductId).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(255).IsRequired();
        builder.Property(s => s.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(s => s.Quantity).IsRequired();

        // Characteristics — OWNED TYPE для Sku
        // Отдельная таблица "sku_characteristics"
        builder.OwnsMany(s => s.Characteristics, charBuilder =>
        {
            charBuilder.ToTable("sku_characteristics");
            charBuilder.WithOwner().HasForeignKey("SkuId");
            charBuilder.HasKey(c => c.Id);
            charBuilder.Property(c => c.Name).HasMaxLength(100).IsRequired();
            charBuilder.Property(c => c.Value).HasMaxLength(255).IsRequired();
        });

        builder.Metadata
            .FindNavigation(nameof(Sku.Characteristics))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.Url).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Order).IsRequired();
    }
}

// CharacteristicConfiguration больше НЕ НУЖНА — она конфигурируется внутри OwnsMany