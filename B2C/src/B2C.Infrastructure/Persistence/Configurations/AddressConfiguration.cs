using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Addresses;
using B2C.Domain.Buyers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2C.Infrastructure.Persistence.Configurations
{
    public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.ToTable("addresses");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(a => a.BuyerId).HasColumnName("buyer_id").IsRequired();
            builder.Property(a => a.Country).HasColumnName("country").IsRequired().HasMaxLength(100);
            builder.Property(a => a.City).HasColumnName("city").IsRequired().HasMaxLength(100);
            builder.Property(a => a.Street).HasColumnName("street").IsRequired().HasMaxLength(200);
            builder.Property(a => a.House).HasColumnName("house").HasMaxLength(20);
            builder.Property(a => a.Apartment).HasColumnName("apartment").HasMaxLength(20);
            builder.Property(a => a.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
            builder.Property(a => a.IsDefault).HasColumnName("is_default").IsRequired().HasDefaultValue(false);

            builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
            builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

            builder.Ignore(a => a.DomainEvents);

            builder.HasIndex(a => a.BuyerId).HasDatabaseName("ix_addresses_buyer");

            // Partial unique index: не более одного default-адреса на покупателя.
            // Это БД-гарантия инварианта "максимум один default", который Application
            // поддерживает через UnsetDefaultExceptAsync. Двойная защита.
            builder.HasIndex(a => a.BuyerId)
                .IsUnique()
                .HasDatabaseName("ux_addresses_one_default_per_buyer")
                .HasFilter("is_default = true");

            builder.HasOne<Buyer>()
                .WithMany()
                .HasForeignKey(a => a.BuyerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
