using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Products
{
    /// <summary>
    /// Internal Entity внутри агрегата Product. Замечание модератора по конкретному
    /// полю товара или конкретному SKU.
    /// 
    /// Имеет Id (Identity), потому что:
    /// - история (несколько раундов модерации = несколько FieldReport с одной field_name)
    /// - в API могут понадобиться операции типа "удалить устаревшее замечание"
    /// 
    /// Жизненный цикл — внутри агрегата Product. Управляется только через методы
    /// Product. Не имеет своего Repository.
    /// </summary>
    public sealed class FieldReport :Entity<Guid>
    {
        public Guid ProductId { get; private set; }
        public FieldReportTarget FieldName { get; private set; }
        public Guid? SkuId { get; private set; }
        public string Comment { get; private set; } = null!;

        public int ModerationRound {  get; private set; }
        public DateTime ReportedAt { get; private set; }



        public FieldReport() { }
        public FieldReport(
        Guid id,
        Guid productId,
        FieldReportTarget fieldName,
        Guid? skuId,
        string comment,
        int moderationRound,
        DateTime reportedAt)
        : base(id)
        {
            if (productId == Guid.Empty)
                throw new DomainException("ProductId is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(comment))
                throw new DomainException("FieldReport comment is required", "INVALID_REQUEST");
            if (moderationRound <= 0)
                throw new DomainException("ModerationRound must be positive", "INVALID_REQUEST");

            // Бизнес-инвариант: если field — про SKU, sku_id обязателен.
            if (IsSkuField(fieldName) && skuId is null)
                throw new DomainException(
                    $"sku_id is required for field {fieldName}",
                    "INVALID_REQUEST");

            if (!IsSkuField(fieldName) && skuId is not null)
                throw new DomainException(
                    $"sku_id must be null for field {fieldName}",
                    "INVALID_REQUEST");

            ProductId = productId;
            FieldName = fieldName;
            SkuId = skuId;
            Comment = comment;
            ModerationRound = moderationRound;
            ReportedAt = reportedAt;
        }
        private static bool IsSkuField(FieldReportTarget f) => f is FieldReportTarget.SkuName
            or FieldReportTarget.SkuImage
            or FieldReportTarget.SkuPrice;
    }
}
