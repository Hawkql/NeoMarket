using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Детальная карточка товара (US-CAT-03). Включает SKU.
    /// ACL: cost_price и reserved_quantity физически отсутствуют в типе.
    /// </summary>
    public sealed record ProductDetail(
        Guid Id,
        string Title,
        string Description,
        Guid CategoryId,
        IReadOnlyList<string> ImageUrls,
        IReadOnlyList<SkuInfo> Skus,
        IReadOnlyList<CharacteristicValue> Characteristics,
        double? Rating,
        int? ReviewsCount);
}
