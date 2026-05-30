using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace B2C.Application.Integration.Dtos
{
    /// <summary>Страница каталога с пагинацией.</summary>
    public sealed record CatalogPage(
        IReadOnlyList<ProductSummary> Items,
        int TotalCount,
        int Limit,
        int Offset);
}
