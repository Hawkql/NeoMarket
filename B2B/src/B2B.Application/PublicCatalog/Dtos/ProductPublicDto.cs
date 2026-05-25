using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Products;

namespace B2B.Application.PublicCatalog.Dtos
{
    public sealed record ProductPublicDto(
        Guid Id,
        Guid SellerId,
        Guid CategoryId,
        string Title,
        string Slug,
        string Description,
        ProductStatus Status,
        IReadOnlyList<PublicImageDto> Images,
        IReadOnlyList<PublicCharacteristicDto> Characteristics,
        IReadOnlyList<SkuPublicDto> Skus,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
public sealed record PublicImageDto(Guid Id, string Url, int Ordering);
public sealed record PublicCharacteristicDto(Guid Id, string Name, string Value);