using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Sellers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations
{
    public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(t => t.SellerId).HasColumnName("seller_id").IsRequired();
            builder.Property(t => t.TokenHash).HasColumnName("token_hash").IsRequired().HasMaxLength(128);
            builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(t => t.Revoked).HasColumnName("revoked").IsRequired().HasDefaultValue(false);
            builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

            // Поиск токена по хэшу при refresh/logout — должен быть быстрым и уникальным
            builder.HasIndex(t => t.TokenHash)
                .IsUnique()
                .HasDatabaseName("ux_refresh_tokens_hash");

            // Поиск активных токенов продавца
            builder.HasIndex(t => t.SellerId)
                .HasDatabaseName("ix_refresh_tokens_seller");
        }
    }
}
