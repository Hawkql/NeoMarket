using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Common.Pagination;
using MediatR;

namespace B2C.Application.Catalog.Queries.ListCatalogProducts
{
    /// <summary>
    /// Список товаров каталога с фильтрами/поиском/сортировкой/пагинацией.
    /// B2C-1 + B2C-2 (поиск передаётся в Search).
    /// </summary>
    public sealed record ListCatalogProductsQuery(
        Guid? CategoryId,
        string? Search,
        IReadOnlyDictionary<string, string>? Filters,
        int? MinPrice,
        int? MaxPrice,
        CatalogSortDto Sort,
        int Limit,
        int Offset) : IRequest<PagedResult<CatalogProductCardDto>>;
}
