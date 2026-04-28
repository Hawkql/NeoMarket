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
    internal sealed class SkuConfiguration : IEntityTypeConfiguration<Sku>
    {
        public void Configure(EntityTypeBuilder<Sku> builder)
        {
            builder.ToTable("skus");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();
            builder.Property(s => s.Name).HasMaxLength(500).IsRequired();
            builder.Property(s => s.Price).HasPrecision(18, 2).IsRequired();
            builder.Property(s => s.Quantity).HasDefaultValue(0);
            builder.HasOne(s => s.Product)
                .WithMany("_skus")
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(s => s.ProductId);

            builder.Navigation(s => s.Characteristics).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(s => s.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
