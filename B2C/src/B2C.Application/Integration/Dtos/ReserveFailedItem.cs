using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Детали по одной неуспешной позиции из ReserveResult.
    /// Requested — сколько просили, Available — сколько реально доступно (для UI-сообщения).
    /// </summary>
    public sealed record ReserveFailedItem(
        Guid SkuId,
        int Requested,
        int Available,
        ReserveFailReason Reason);
}
