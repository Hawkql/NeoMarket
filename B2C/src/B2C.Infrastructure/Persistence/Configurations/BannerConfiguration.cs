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
    public sealed class BannerConfiguration : IEntityTypeConfiguration<Banner>
    {
        public void Configure(EntityTypeBuilder<Banner> builder)
        {
            builder.ToTable("banners");
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(b => b.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
            builder.Property(b => b.ImageUrl).HasColumnName("image_url").IsRequired().HasMaxLength(2000);
            builder.Property(b => b.LinkUrl).HasColumnName("link_url").HasMaxLength(2000);
            builder.Property(b => b.Priority).HasColumnName("priority").IsRequired().HasDefaultValue(0);
            builder.Property(b => b.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
            builder.Property(b => b.StartsAt).HasColumnName("starts_at").HasColumnType("timestamptz");
            builder.Property(b => b.EndsAt).HasColumnName("ends_at").HasColumnType("timestamptz");

            builder.Property(b => b.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(b => b.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(b => b.DomainEvents);

            // Индекс для выборки видимых баннеров по priority.
            builder.HasIndex(b => new { b.IsActive, b.Priority })
                .HasDatabaseName("ix_banners_active_priority");
        }
    }
}
