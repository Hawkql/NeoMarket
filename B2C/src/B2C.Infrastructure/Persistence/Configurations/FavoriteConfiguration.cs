using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Buyers;
using B2C.Domain.Favorites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2C.Infrastructure.Persistence.Configurations
{
    public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
    {
        public void Configure(EntityTypeBuilder<Favorite> builder)
        {
            builder.ToTable("favorites");
            builder.HasKey(f => f.Id);

            builder.Property(f => f.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(f => f.BuyerId).HasColumnName("buyer_id").IsRequired();
            builder.Property(f => f.ProductId).HasColumnName("product_id").IsRequired();

            builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(f => f.DomainEvents);

            // Уникальность пары (buyer, product) — нельзя добавить один товар дважды.
            // Это БД-гарантия идемпотентности AddFavorite.
            builder.HasIndex(f => new { f.BuyerId, f.ProductId })
                .IsUnique()
                .HasDatabaseName("ux_favorites_buyer_product");

            // Индекс для bulk-удаления по product_id (RemoveAllByProductAsync при ProductDeleted).
            builder.HasIndex(f => f.ProductId)
                .HasDatabaseName("ix_favorites_product");

            builder.HasOne<Buyer>()
                .WithMany()
                .HasForeignKey(f => f.BuyerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
