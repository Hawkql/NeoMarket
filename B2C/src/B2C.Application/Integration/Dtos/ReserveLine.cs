using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>Позиция для reserve/unreserve/fulfill.</summary>
    public sealed record ReserveLine(Guid SkuId, int Quantity);
}
