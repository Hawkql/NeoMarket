using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("products");
            builder.HasKey(x => x.Id);
            builder.Property(p=>p.Title).HasMaxLength(255).IsRequired();
            builder.Property(p=>p.Description).HasMaxLength(3000);
            builder.Property(p => p.Slug).HasMaxLength(255);
            builder.Property(p => p.Status).HasConversion<int>();

            builder.Ignore(p => p.DomainEvents);
            builder.HasMany(p=>p.Skus)
                .WithOne()
                .HasForeignKey(p=>p.ProductId);

            builder.Metadata
                .FindNavigation(nameof(Product.Id))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
