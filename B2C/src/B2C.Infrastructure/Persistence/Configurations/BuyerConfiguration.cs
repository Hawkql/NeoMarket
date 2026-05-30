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
    public sealed class BuyerConfiguration : IEntityTypeConfiguration<Buyer>
    {
        public void Configure(EntityTypeBuilder<Buyer> builder)
        {
            builder.ToTable("buyers");
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(b => b.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            builder.Property(b => b.PasswordHash).HasColumnName("password_hash").IsRequired();
            builder.Property(b => b.FirstName).HasColumnName("first_name").HasMaxLength(100);
            builder.Property(b => b.LastName).HasColumnName("last_name").HasMaxLength(100);
            builder.Property(b => b.Phone).HasColumnName("phone").HasMaxLength(32);
            builder.Property(b => b.Deleted).HasColumnName("deleted").IsRequired().HasDefaultValue(false);

            builder.Property(b => b.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(b => b.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(b => b.DomainEvents);

            // Уникальность email среди не удалённых аккаунтов (partial unique index).
            // Soft-deleted покупатели не блокируют повторную регистрацию того же email.
            builder.HasIndex(b => b.Email)
                .IsUnique()
                .HasDatabaseName("ux_buyers_email")
                .HasFilter("deleted = false");
        }
    }
}
