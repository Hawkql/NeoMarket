using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;

namespace B2B.Application.PublicCatalog.Dtos
{
    internal static class PublicCatalogMapper
    {
        public static SkuPublicDto MapSku(Sku sku, IReadOnlyCollection<Image> images)
        {
            return new SkuPublicDto(
                Id: sku.Id,
                ProductId: sku.ProductId,
                Name: sku.Name,
                Price: sku.Price,
                Discount: sku.Discount,
                StockQuantity: sku.ActiveQuantity + sku.ReservedQuantity,
                ActiveQuantity: sku.ActiveQuantity,
                Article: sku.Article,
                Images: images
                    .OrderBy(i => i.Ordering)
                    .Select(i => new PublicImageDto(i.Id, i.Url, i.Ordering))
                    .ToList(),
                Characteristics: sku.Characteristics
                    .Select(c => new PublicCharacteristicDto(c.Id, c.Name, c.Value))
                    .ToList());
            // ВНИМАНИЕ: cost_price и reserved_quantity НЕ включены — витрина их не видит
        }

        public static ProductPublicDto MapProduct(
            Product product,
            IReadOnlyCollection<Image> productImages,
            IReadOnlyCollection<Sku> liveSkus,
            IReadOnlyDictionary<Guid, IReadOnlyCollection<Image>> skuImagesMap)
        {
            return new ProductPublicDto(
                Id: product.Id,
                SellerId: product.SellerId,
                CategoryId: product.CategoryId,
                Title: product.Title,
                Slug: product.Slug,
                Description: product.Description,
                Status: product.Status,
                Images: productImages
                    .OrderBy(i => i.Ordering)
                    .Select(i => new PublicImageDto(i.Id, i.Url, i.Ordering))
                    .ToList(),
                Characteristics: product.Characteristics
                    .Select(c => new PublicCharacteristicDto(c.Id, c.Name, c.Value))
                    .ToList(),
                Skus: liveSkus
                    .Select(s =>
                    {
                        var imgs = skuImagesMap.TryGetValue(s.Id, out var list)
                            ? list
                            : Array.Empty<Image>();
                        return MapSku(s, imgs);
                    })
                    .ToList(),
                CreatedAt: product.CreatedAt,
                UpdatedAt: product.UpdatedAt);
        }
    }
}
