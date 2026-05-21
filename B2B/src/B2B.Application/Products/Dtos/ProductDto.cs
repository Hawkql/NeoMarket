using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Products.Dtos
{
    public sealed record ProductDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    bool Deleted,
    bool Blocked,
    Guid CategoryId,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<CharacteristicDto> Characteristics,
    IReadOnlyList<SkuDto> Skus,
    DateTime CreatedAt,
    DateTime UpdatedAt);

    public sealed record ImageDto(Guid Id, string Url, int Ordering);
    public sealed record CharacteristicDto(string Name, string Value);
    public sealed record SkuDto(
    Guid Id,
    string Name,
    int Price,
    int CostPrice,
    int Discount,
    string Image,
    int ActiveQuantity,
    int ReservedQuantity);
}
