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

            builder.Property(i => i.SellerId)
                .HasColumnName("seller_id")
                .IsRequired();



            builder.Property(i => i.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(i => i.AcceptedAt)
                .HasColumnName("accepted_at")
                .HasColumnType("timestamptz");
            builder.Property(i => i.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamptz")
                .IsRequired();
            builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

            // НЕ сохраняем DomainEvents — это transient
            builder.Ignore(i => i.DomainEvents);

            // Связь с InvoiceLine (строки накладной)
            builder.HasMany(i => i.Items)
                 .WithOne()
                 .HasForeignKey(item => item.InvoiceId)
                 .OnDelete(DeleteBehavior.Cascade);

            // Указываем, что Lines заполняется через backing field _item
            builder.Navigation(i => i.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(i => new { i.SellerId, i.Status, i.CreatedAt })
            .HasDatabaseName("ix_invoices_seller_status_created");

            // Накладные в статусе PENDING (для очереди оператора склада)
            builder.HasIndex(i => i.Status)
                .HasDatabaseName("ix_invoices_status_pending")
                .HasFilter("status = 'Pending'");

            builder.ToTable(t =>
            {
                // Если статус не Pending — обязательно accepted_at должен быть заполнен
                t.HasCheckConstraint(
                    "ck_invoices_accepted_at_consistency",
                    "(status = 'Pending' AND accepted_at IS NULL) OR " +
                    "(status <> 'Pending' AND accepted_at IS NOT NULL)");
            });
        }
    }
}
