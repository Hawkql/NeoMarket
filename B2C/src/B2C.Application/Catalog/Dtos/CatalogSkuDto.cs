using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Catalog.Dtos
{
    public sealed record CatalogSkuDto(
        Guid Id,
        string Name,
        int Price,
        int? Discount,
        string? ImageUrl,
        bool InStock,
        IReadOnlyList<CatalogCharacteristicDto> Characteristics);
}
