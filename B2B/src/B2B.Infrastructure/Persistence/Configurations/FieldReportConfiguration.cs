using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Infrastructure.Persistence.Configurations
{
    public sealed class FieldReportConfiguration : IEntityTypeConfiguration<FieldReport>
    {
        public void Configure(EntityTypeBuilder<FieldReport> builder)
        {
            builder.ToTable("field_reports");

            builder.HasKey(fr => fr.Id);

            builder.Property(fr => fr.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(fr => fr.ProductId)
                .HasColumnName("product_id")
                .IsRequired();

            // FieldName хранится строкой (как Status у Product)
            builder.Property(fr => fr.FieldName)
                .HasColumnName("field_name")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            // Nullable — если замечание относится к Product, а не к SKU
            builder.Property(fr => fr.SkuId)
                .HasColumnName("sku_id");

            builder.Property(fr => fr.Comment)
                .HasColumnName("comment")
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(fr => fr.ModerationRound)
                .HasColumnName("moderation_round")
                .IsRequired();

            builder.Property(fr => fr.ReportedAt)
                .HasColumnName("reported_at")
                .HasColumnType("timestamptz")
                .IsRequired();

            // Индекс для запроса "все замечания по товару"
            builder.HasIndex(fr => fr.ProductId)
                .HasDatabaseName("ix_field_reports_product");

            // Индекс для запроса "все замечания по SKU"
            builder.HasIndex(fr => fr.SkuId)
                .HasDatabaseName("ix_field_reports_sku");

            // Check constraint — moderation_round положительный
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "ck_field_reports_round_positive",
                    "moderation_round > 0");
            });
        }
    }
}
