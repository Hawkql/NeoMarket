using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using MediatR;

namespace B2C.Application.Catalog.Queries.GetFacets
{
    /// <summary>
    /// Фасеты для текущей выборки: количество товаров по каждому значению фильтра
    /// с учётом уже применённых фильтров. Например: выбрали "Apple" — счётчики
    /// для других брендов перестраиваются с учётом этой выборки.
    /// </summary>
    public sealed record GetFacetsQuery(
        Guid? CategoryId,
        string? Search,
        IReadOnlyDictionary<string, string>? AppliedFilters,
        int? MinPrice,
        int? MaxPrice) : IRequest<CategoryFiltersDto>;
}
