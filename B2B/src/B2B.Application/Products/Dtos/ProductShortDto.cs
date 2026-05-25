using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Products;

namespace B2B.Application.Products.Dtos
{
    public sealed record ProductShortDto(
    Guid Id,
    string Title,
    string Slug,
    ProductStatus Status,
    Guid CategoryId,
    bool Deleted,
    DateTime CreatedAt,
    int? MinPrice,
    string? CoverImage);
}
