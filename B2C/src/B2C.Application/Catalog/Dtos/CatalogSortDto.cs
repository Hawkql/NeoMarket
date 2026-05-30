using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Catalog.Dtos
{
    /// <summary>
    /// API-уровневый sort. Мапится в Integration.CatalogSort в handler'е.
    /// Разделение нужно, чтобы при изменении внутреннего контракта B2B
    /// не ломать публичный API B2C.
    /// </summary>
    public enum CatalogSortDto
    {
        Rating = 0,
        Popularity = 1,
        PriceAsc = 2,
        PriceDesc = 3,
        DateDesc = 4,
        DiscountDesc = 5,
    }
}
