using System;
using System.Collections.Generic;

namespace B2C.Application.Catalog.Dtos
{
    /// <summary>
    /// openapi: CatalogSku. required: id, price, available_quantity.
    /// attributes — произвольный объект характеристик (key→value).
    /// </summary>
    public sealed record CatalogSkuDto(
        Guid Id,
        string? Name,
        string? SkuCode,
        int Price,
        int? OldPrice,
        int AvailableQuantity,
        IReadOnlyDictionary<string, string>? Attributes,
        IReadOnlyList<ImageRefDto> Images);
}