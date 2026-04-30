using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations
{
    internal class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("invoices");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.SellerId).IsRequired();

            builder.Property(i => i.Number)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(i => i.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(i => i.CreateAt).IsRequired();
            builder.Property(i => i.AcceptedAt);

            // НЕ сохраняем DomainEvents — это transient
            builder.Ignore(i => i.DomainEvents);

            // Связь с InvoiceLine (строки накладной)
            builder.HasMany(i => i.Lines)
                .WithOne()
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Указываем, что Lines заполняется через backing field _lines
            builder.Metadata
                .FindNavigation(nameof(Invoice.Lines))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            // Индекс по продавцу — продавец часто запрашивает свои накладные
            builder.HasIndex(i => i.SellerId)
                .HasDatabaseName("ix_invoices_seller_id");
        }
    }

    internal class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
    {
        public void Configure(EntityTypeBuilder<InvoiceLine> builder)
        {
            builder.ToTable("invoice_lines");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.InvoiceId).IsRequired();
            builder.Property(l => l.SkuId).IsRequired();
            builder.Property(l => l.Quantity).IsRequired();

            builder.Property(l => l.Cost)
                .HasPrecision(18, 2)
                .IsRequired();
        }
    }
}
