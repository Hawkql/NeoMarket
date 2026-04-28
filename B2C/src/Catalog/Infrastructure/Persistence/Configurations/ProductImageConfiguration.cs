using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.ToTable("product_images");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Url).HasMaxLength(2048).IsRequired();
            builder.Property(i => i.Order).HasDefaultValue(0);
            builder.HasIndex(i => new { i.ProductId, i.Order });
            builder.HasIndex(i => new { i.SkuId, i.Order });
        }
    }
}
