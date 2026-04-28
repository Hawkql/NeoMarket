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
    internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("categories");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).ValueGeneratedNever();
            builder.Property(c => c.Name).HasMaxLength(255).IsRequired();
            builder.Property(c => c.Slug).HasMaxLength(255).IsRequired();
            builder.HasIndex(c => c.Slug).IsUnique();
            builder.Property(c => c.Description).HasColumnType("text");
            builder.Property(c => c.ImageUrl).HasMaxLength(2048);
            builder.Property(c => c.SeoTitle).HasMaxLength(255);
            builder.Property(c => c.SeoDescription).HasMaxLength(512);
            builder.Property(c => c.SeoKeywordsJson).HasColumnType("text");
            builder.Property(c => c.OgTitle).HasMaxLength(255);
            builder.Property(c => c.OgDescription).HasMaxLength(512);
            builder.Property(c => c.OgImage).HasMaxLength(2048);
            builder.Property(c => c.TwitterCard).HasMaxLength(50);
            builder.HasOne(c => c.Parent)
                .WithMany("_children")
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            builder.HasIndex(c => c.ParentId);
            builder.HasIndex(c => c.IsActive);

            builder.Navigation(c => c.Children).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(c => c.Filters).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }

}
