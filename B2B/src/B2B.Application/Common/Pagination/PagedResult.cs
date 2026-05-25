using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Common.Pagination
{
    public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Limit,
    int Offset);
}
