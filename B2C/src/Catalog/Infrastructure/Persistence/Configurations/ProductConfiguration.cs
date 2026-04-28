using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("products");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).ValueGeneratedNever();
            builder.Property(p => p.Slug).HasMaxLength(255).IsRequired();
            builder.HasIndex(p => p.Slug).IsUnique();
            builder.Property(p => p.Title).HasMaxLength(500).IsRequired();
            builder.Property(p => p.Description).HasColumnType("text");
            builder.Property(p => p.Status)
                .HasConversion(
                    v => v.ToString().ToUpperInvariant(),
                    v => Enum.Parse<ProductStatus>(v, true))
                .HasMaxLength(50);
            builder.Property(p => p.CategoryId);
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");
            builder.HasIndex(p => p.CategoryId);
            builder.HasIndex(p => p.Status);
            builder.HasIndex(p => p.CreatedAt);

            // Приватные коллекции — указываем backing field явно
            builder.Navigation(p => p.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(p => p.Characteristics).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(p => p.Skus).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
