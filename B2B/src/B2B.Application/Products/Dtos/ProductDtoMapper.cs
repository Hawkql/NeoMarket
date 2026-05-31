using System;
using System.Collections.Generic;
using System.Linq;
using B2B.Application.Skus.Dtos;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;

namespace B2B.Application.Products.Dtos
{
    internal static class ProductDtoMapper
    {
        public static ProductDto Map(
            Product product,
            IReadOnlyCollection<Image> productImages,
            IReadOnlyCollection<Sku> skus,
            IReadOnlyDictionary<Guid, IReadOnlyCollection<Image>>? skuImages = null)
        {
            skuImages ??= new Dictionary<Guid, IReadOnlyCollection<Image>>();

            return new ProductDto(
                Id: product.Id,
                SellerId: product.SellerId,
                CategoryId: product.CategoryId,
                Title: product.Title,
                Slug: product.Slug,
                Description: product.Description,
                Status: product.Status,
                Deleted: product.Deleted,
                BlockingReasonId: product.BlockingReason?.ReasonId,
                ModeratorComment: product.BlockingReason?.Comment,
                Images: productImages
                    .OrderBy(i => i.Ordering)
                    .Select(i => new ImageDto(i.Id, i.Url, i.Ordering))
                    .ToList(),
                Characteristics: product.Characteristics
                    .Select(c => new CharacteristicDto(c.Id, c.Name, c.Value))
                    .ToList(),
                Skus: skus
                    .Where(s => !s.Deleted)
                    .Select(s => SkuDtoMapper.Map(
                        s,
                        skuImages.TryGetValue(s.Id, out var imgs) ? imgs : Array.Empty<Image>()))
                    .ToList(),
                CreatedAt: product.CreatedAt,
                UpdatedAt: product.UpdatedAt,
                Blocked: product.Blocked,
                FieldReports: product.FieldReports
                    .OrderBy(fr => fr.ReportedAt)
                    .Select(fr => new FieldReportDto(
                        FieldName: MapFieldName(fr.FieldName),
                        SkuId: fr.SkuId,
                        Comment: fr.Comment))
                    .ToList());
        }

        private static string MapFieldName(FieldReportTarget target) => target switch
        {
            FieldReportTarget.Title => "title",
            FieldReportTarget.Description => "description",
            FieldReportTarget.ProductImages => "product_images",
            FieldReportTarget.Category => "category",
            FieldReportTarget.SkuName => "sku_name",
            FieldReportTarget.SkuImage => "sku_image",
            FieldReportTarget.SkuPrice => "sku_price",
            _ => target.ToString().ToLowerInvariant()
        };
    }
}