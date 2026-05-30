using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Catalog.Dtos
{
    /// <summary>
    /// Карточка товара в списках каталога, поиска, подборок.
    /// Формат фиксирован в OpenAPI (B2C-1, B2C-2, B2C-15).
    /// </summary>
    public sealed record CatalogProductCardDto(
        Guid Id,
        string Title,
        string? ImageUrl,
        int Price,
        int? OldPrice,
        int? Discount,
        bool InStock,
        double? Rating,
        int? ReviewsCount);
}
