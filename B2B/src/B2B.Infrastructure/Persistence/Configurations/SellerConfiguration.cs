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
    public sealed class SellerConfiguration : IEntityTypeConfiguration<Seller>
    {
        public void Configure(EntityTypeBuilder<Seller> builder)
        {
            builder.ToTable("sellers");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(s => s.Email).HasColumnName("email").IsRequired().HasMaxLength(320);
            builder.Property(s => s.PasswordHash).HasColumnName("password_hash").IsRequired();
            builder.Property(s => s.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
            builder.Property(s => s.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
            builder.Property(s => s.MiddleName).HasColumnName("middle_name").HasMaxLength(100);
            builder.Property(s => s.CompanyName).HasColumnName("company_name").IsRequired().HasMaxLength(255);
            builder.Property(s => s.Inn).HasColumnName("inn").IsRequired().HasMaxLength(12);
            builder.Property(s => s.Phone).HasColumnName("phone").HasMaxLength(20);
            builder.Property(s => s.Deleted).HasColumnName("deleted").IsRequired().HasDefaultValue(false);

            builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(s => s.DomainEvents);

            // Уникальность email среди не удалённых аккаунтов (partial unique index)
            builder.HasIndex(s => s.Email)
                .IsUnique()
                .HasDatabaseName("ux_sellers_email")
                .HasFilter("deleted = false");
        }
    }
}
