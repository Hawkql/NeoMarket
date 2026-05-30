using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>Сортировка каталога (US-CAT-01).</summary>
    public enum CatalogSort
    {
        Rating = 0,          // default
        Popularity = 1,
        PriceAsc = 2,
        PriceDesc = 3,
        DateDesc = 4,
        DiscountDesc = 5,
    }
}
