using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Параметры запроса каталога (US-CAT-01, US-CAT-02).
    /// Parameter Object: фильтрация, сортировка, пагинация.
    /// 
    /// Filters передаётся как Dictionary, потому что набор фильтров динамический
    /// (зависит от категории — приходит из B2B). Ключ — slug характеристики, значение — выбранное.
    /// </summary>
    public sealed record CatalogQuery(
        Guid? CategoryId,
        string? Search,
        IReadOnlyDictionary<string, string>? Filters,
        int? MinPrice,
        int? MaxPrice,
        CatalogSort Sort,
        int Limit,
        int Offset);
}
