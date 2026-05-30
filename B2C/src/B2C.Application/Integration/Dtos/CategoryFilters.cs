using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Доступные фильтры для категории + диапазон цены.
    /// US-CAT-01: фильтры на боковой панели каталога.
    /// </summary>
    public sealed record CategoryFilters(
        IReadOnlyList<FilterDefinition> Filters,
        int? PriceMin,
        int? PriceMax);
}
