using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.PublicCatalog.Dtos
{
    public sealed record SkuPublicDto(
    Guid Id,
    Guid ProductId,
    string Name,
    int Price,
    int Discount,
    int StockQuantity,      // active + reserved
    int ActiveQuantity,     // доступно к покупке
    string? Article,
    IReadOnlyList<PublicImageDto> Images,
    IReadOnlyList<PublicCharacteristicDto> Characteristics);

}
