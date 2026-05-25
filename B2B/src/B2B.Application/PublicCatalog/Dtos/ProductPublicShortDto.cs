using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Products;

namespace B2B.Application.PublicCatalog.Dtos
{
    public sealed record ProductPublicShortDto(
    Guid Id,
    string Title,
    string Slug,
    ProductStatus Status,
    Guid CategoryId,
    int? MinPrice,
    string? CoverImage,
    DateTime CreatedAt);
}
