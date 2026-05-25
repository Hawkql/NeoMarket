using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Skus.Dtos
{
    public sealed record SkuResponseDto(
    Guid Id,
    Guid ProductId,
    string Name,
    int Price,
    int Discount,
    int? CostPrice,
    int StockQuantity,        // active + reserved (всего на складе)
    int ActiveQuantity,       // доступно к продаже
    int ReservedQuantity,     // зарезервировано
    string? Article,
    IReadOnlyList<SkuImageDto> Images,
    IReadOnlyList<SkuCharacteristicDto> Characteristics,
    DateTime CreatedAt,
    DateTime UpdatedAt);

    public sealed record SkuImageDto(Guid Id, string Url, int Ordering);
    public sealed record SkuCharacteristicDto(Guid Id, string Name, string Value);
}
