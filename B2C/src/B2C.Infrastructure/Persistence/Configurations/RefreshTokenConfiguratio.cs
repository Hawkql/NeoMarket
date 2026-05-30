using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Buyers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2C.Infrastructure.Persistence.Configurations
{
    public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(t => t.BuyerId).HasColumnName("buyer_id").IsRequired();
            builder.Property(t => t.TokenHash).HasColumnName("token_hash").IsRequired().HasMaxLength(128);
            builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(t => t.Revoked).HasColumnName("revoked").IsRequired().HasDefaultValue(false);
            builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

            // RefreshToken не AggregateRoot (наследует Entity), DomainEvents у него нет —
            // Ignore не нужен.

            // Поиск по хэшу при /refresh и /logout — частая операция, нужен индекс.
            builder.HasIndex(t => t.TokenHash)
                .HasDatabaseName("ix_refresh_tokens_hash");

            // FK на покупателя + индекс для bulk-revoke (RevokeAllForBuyerAsync).
            builder.HasIndex(t => t.BuyerId)
                .HasDatabaseName("ix_refresh_tokens_buyer");

            builder.HasOne<Buyer>()
                .WithMany()
                .HasForeignKey(t => t.BuyerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
