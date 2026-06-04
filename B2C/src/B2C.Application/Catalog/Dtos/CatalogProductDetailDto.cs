using System;
using System.Collections.Generic;

namespace B2C.Application.Catalog.Dtos
{
    /// <summary>
    /// openapi: CatalogProductDetail = CatalogProductCard + description + attributes + skus.
    /// Плоская структура: повторяем поля карточки сверху и добавляем расширения снизу.
    /// </summary>
    public sealed record CatalogProductDetailDto(
        // === поля CatalogProductCard ===
        Guid Id,
        string Name,
        string? Slug,
        CategoryRefDto? Category,
        int MinPrice,
        int? OldPrice,
        bool HasStock,
        double? Rating,
        int ReviewsCount,
        IReadOnlyList<ImageRefDto> Images,
        // === расширения CatalogProductDetail ===
        string Description,
        IReadOnlyDictionary<string, string>? Attributes,
        IReadOnlyList<CatalogSkuDto> Skus);
}