using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Products
{
    /// <summary>
    /// Фильтры витрины (Parameter Object). Все опциональны.
    /// Сортировка — enum PublicSort.
    /// </summary>
    public sealed record PublicCatalogFilter(
        Guid? CategoryId,
        IReadOnlyCollection<Guid>? CategoryIdsInTree,  // категория + потомки
        string? Search,
        int? MinPrice,
        int? MaxPrice,
        Guid? SellerId,
        PublicSort Sort,
        int Limit,
        int Offset);

    public enum PublicSort
    {
        CreatedDesc,
        PriceAsc,
        PriceDesc
        // popular → маппится в CreatedDesc на уровне Application (нет данных)
    }
}

