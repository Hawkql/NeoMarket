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

    /// <summary>
    /// EF Core mapping для InvoiceItem — internal entity агрегата Invoice.
    /// 
    /// Имеет свой Id (Guid), отдельную таблицу invoice_items.
    /// НЕ имеет своего DbSet в DbContext — доступ только через Invoice.Items.
    /// Связь с Invoice через FK с CASCADE delete (внутри одного агрегата).
    /// Связь с SKU — только через SkuId (Reference by Identity).
    /// </summary>
    public sealed class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
    {
        public void Configure(EntityTypeBuilder<InvoiceItem> builder)
        {
            builder.ToTable("invoice_items");

            builder.HasKey(item => item.Id);

            builder.Property(item => item.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            // Ссылка на накладную (FK создаётся через InvoiceConfiguration.HasMany)
            builder.Property(item => item.InvoiceId)
                .HasColumnName("invoice_id")
                .IsRequired();

            // Ссылка на SKU — только Guid, без навигации (Reference by Identity)
            builder.Property(item => item.SkuId)
                .HasColumnName("sku_id")
                .IsRequired();

            // Заявлено продавцом (immutable после создания)
            builder.Property(item => item.Quantity)
                .HasColumnName("quantity")
                .IsRequired();

            // Принято оператором (nullable до приёмки)
            builder.Property(item => item.AcceptedQuantity)
                .HasColumnName("accepted_quantity");
            // Не IsRequired — nullable

            // ====================================================================
            // ИНДЕКСЫ
            // ====================================================================

            // UNIQUE: в одной накладной не может быть двух строк с одинаковым SKU
            builder.HasIndex(item => new { item.InvoiceId, item.SkuId })
                .IsUnique()
                .HasDatabaseName("ux_invoice_items_invoice_sku");

            // Для аудита "когда этот SKU поставлялся"
            builder.HasIndex(item => item.SkuId)
                .HasDatabaseName("ix_invoice_items_sku");

            // ====================================================================
            // CHECK CONSTRAINTS
            // ====================================================================
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "ck_invoice_items_quantity_positive",
                    "quantity > 0");

                // Принято — либо null, либо в диапазоне 0..quantity
                t.HasCheckConstraint(
                    "ck_invoice_items_accepted_quantity_valid",
                    "accepted_quantity IS NULL OR " +
                    "(accepted_quantity >= 0 AND accepted_quantity <= quantity)");
            });
        }
    }
}
