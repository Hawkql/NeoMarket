using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace B2C.Application.Common.Pagination
{
    /// <summary>
    /// Стандартный результат пагинации. Формат фиксирован в OpenAPI B2C:
    /// { items, total_count, limit, offset }.
    /// </summary>
    public sealed record PagedResult<T>(
        IReadOnlyList<T> Items,
        int TotalCount,
        int Limit,
        int Offset);
}
