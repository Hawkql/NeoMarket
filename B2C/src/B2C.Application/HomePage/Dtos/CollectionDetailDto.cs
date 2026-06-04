using System;
using System.Collections.Generic;
using B2C.Application.Catalog.Dtos;

namespace B2C.Application.HomePage.Dtos
{
    /// <summary>
    /// openapi: Collection. required: id, name, products.
    /// Один тип на список и на «детали» — openapi не различает.
    /// products содержит уже обогащённые CatalogProductCard.
    /// </summary>
    public sealed record CollectionDetailDto(
        Guid Id,
        string Name,
        string? Description,
        IReadOnlyList<CatalogProductCardDto> Products);
}