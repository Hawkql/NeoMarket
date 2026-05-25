using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Images;
using B2B.Domain.Skus;

namespace B2B.Application.Skus.Dtos
{
    internal static class SkuDtoMapper
    {
        public static SkuResponseDto Map(Sku sku, IReadOnlyCollection<Image> images)
        {
            return new SkuResponseDto(
                Id: sku.Id,
                ProductId: sku.ProductId,
                Name: sku.Name,
                Price: sku.Price,
                Discount: sku.Discount,
                CostPrice: sku.CostPrice,
                StockQuantity: sku.ActiveQuantity + sku.ReservedQuantity,
                ActiveQuantity: sku.ActiveQuantity,
                ReservedQuantity: sku.ReservedQuantity,
                Article: sku.Article,
                Images: images
                    .OrderBy(i => i.Ordering)
                    .Select(i => new SkuImageDto(i.Id, i.Url, i.Ordering))
                    .ToList(),
                Characteristics: sku.Characteristics
                    .Select(c => new SkuCharacteristicDto(c.Id, c.Name, c.Value))
                    .ToList(),
                CreatedAt: sku.CreatedAt,
                UpdatedAt: sku.UpdatedAt);
        }
    }
}
