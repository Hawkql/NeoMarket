using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Результат вызова POST /reserve в B2B.
    /// All-or-nothing: либо Success=true и FailedItems пуст,
    /// либо Success=false и FailedItems содержит причины (US-ORD-01).
    /// </summary>
    public sealed record ReserveResult(
        bool Success,
        IReadOnlyList<ReserveFailedItem> FailedItems);
}
