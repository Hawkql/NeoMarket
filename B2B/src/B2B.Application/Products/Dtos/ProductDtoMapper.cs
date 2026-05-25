using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            IReadOnlyCollection<Sku> skus)
        {
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
                    .Select(s => new SkuDto(
                        Id: s.Id,
                        ProductId: s.ProductId,
                        Name: s.Name,
                        Price: s.Price,
                        CostPrice: s.CostPrice??0,
                        Discount: s.Discount,
                        ImageUrl: s.ImageUrl,
                        ActiveQuantity: s.ActiveQuantity,
                        ReservedQuantity: s.ReservedQuantity,
                        Deleted: s.Deleted))
                    .ToList(),
                CreatedAt: product.CreatedAt,
                UpdatedAt: product.UpdatedAt);
        }
    }
}
