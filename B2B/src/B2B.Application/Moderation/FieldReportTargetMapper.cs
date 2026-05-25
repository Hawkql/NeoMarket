using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;
using B2B.Domain.Products;

namespace B2B.Application.Moderation
{
    internal static class FieldReportTargetMapper
    {
        // Строковые имена из контракта Moderation (snake_case) → доменный enum
        public static FieldReportTarget Map(string fieldName)
        {
            return (fieldName ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "title" => FieldReportTarget.Title,
                "description" => FieldReportTarget.Description,
                "product_images" => FieldReportTarget.ProductImages,
                "category" => FieldReportTarget.Category,
                "sku_name" => FieldReportTarget.SkuName,
                "sku_image" => FieldReportTarget.SkuImage,
                "sku_price" => FieldReportTarget.SkuPrice,
                _ => throw new DomainException(
                    $"Unknown field_name: {fieldName}", "INVALID_REQUEST")
            };
        }
    }
}
