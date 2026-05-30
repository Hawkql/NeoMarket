using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Catalog.Dtos
{
    /// <summary>
    /// Детальная карточка товара (B2C-3).
    /// Обращаю внимание: cost_price и reserved_quantity физически отсутствуют —
    /// это и есть ACL на уровне типов (US-CAT-03).
    /// </summary>
    public sealed record CatalogProductDetailDto(
        Guid Id,
        string Title,
        string Description,
        Guid CategoryId,
        IReadOnlyList<string> ImageUrls,
        IReadOnlyList<CatalogSkuDto> Skus,
        IReadOnlyList<CatalogCharacteristicDto> Characteristics,
        double? Rating,
        int? ReviewsCount);
}
