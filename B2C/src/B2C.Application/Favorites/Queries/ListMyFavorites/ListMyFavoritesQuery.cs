using B2C.Application.Catalog.Dtos;
using B2C.Application.Common.Pagination;
using MediatR;

namespace B2C.Application.Favorites.Queries.ListMyFavorites
{
    /// <summary>
    /// openapi: GET /api/v1/favorites → PaginatedCatalogProducts.
    /// Параметры limit/offset переходят из API в query.
    /// </summary>
    public sealed record ListMyFavoritesQuery(int Limit, int Offset)
        : IRequest<PagedResult<CatalogProductCardDto>>;
}