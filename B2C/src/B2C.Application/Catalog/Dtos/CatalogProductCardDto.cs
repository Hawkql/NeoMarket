using System;
using System.Collections.Generic;

namespace B2C.Application.Catalog.Dtos
{
    /// <summary>
    /// Карточка товара в списках (openapi: CatalogProductCard).
    /// required: id, name, min_price, has_stock, images.
    /// </summary>
    public sealed record CatalogProductCardDto(
        Guid Id,
        string Name,
        string? Slug,
        CategoryRefDto? Category,
        int MinPrice,
        int? OldPrice,
        bool HasStock,
        double? Rating,
        int ReviewsCount,
        IReadOnlyList<ImageRefDto> Images);
}