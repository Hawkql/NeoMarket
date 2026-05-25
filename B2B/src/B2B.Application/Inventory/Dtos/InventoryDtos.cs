using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Inventory.Dtos
{
    public sealed record InventoryItemDto(Guid SkuId, int Quantity);

    public sealed record ReserveResultDto(
        Guid OrderId,
        string Status,        // "RESERVED"
        DateTime ReservedAt);
    public sealed record InventoryOrderResultDto(
        Guid OrderId,
        string Status,        // "UNRESERVED" | "FULFILLED"
        DateTime ProcessedAt);
}
