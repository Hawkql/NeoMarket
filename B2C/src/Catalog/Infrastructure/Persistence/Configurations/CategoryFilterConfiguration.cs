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
    internal sealed class CategoryFilterConfiguration : IEntityTypeConfiguration<CategoryFilter>
    {
        public void Configure(EntityTypeBuilder<CategoryFilter> builder)
        {
            builder.ToTable("category_filters");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Slug).HasMaxLength(255).IsRequired();
            builder.Property(f => f.Name).HasMaxLength(255).IsRequired();
            builder.Property(f => f.FilterType).HasMaxLength(20).IsRequired();
            builder.Property(f => f.ValuesJson).HasColumnType("text");
            builder.Property(f => f.MinValue).HasPrecision(18, 2);
            builder.Property(f => f.MaxValue).HasPrecision(18, 2);
            builder.HasOne(f => f.Category)
                .WithMany("_filters")
                .HasForeignKey(f => f.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(f => f.CategoryId);
        }
    }
}
