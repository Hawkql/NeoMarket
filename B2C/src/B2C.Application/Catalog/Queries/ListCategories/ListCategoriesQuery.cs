using System.Collections.Generic;
using B2C.Application.Catalog.Dtos;
using MediatR;

namespace B2C.Application.Catalog.Queries.ListCategories
{
    /// <summary>
    /// openapi: GET /api/v1/catalog/categories — плоский список CategoryRef[].
    /// </summary>
    public sealed record ListCategoriesQuery : IRequest<IReadOnlyList<CategoryRefDto>>;
}