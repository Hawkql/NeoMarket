using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Inventory.Dtos;
using MediatR;

namespace B2B.Application.Inventory.Commands.Reserve
{
    public sealed record ReserveInventoryCommand(
        Guid IdempotencyKey,
        Guid OrderId,
        IReadOnlyList<InventoryItemDto> Items
    ) : IRequest<ReserveResultDto>;
}
