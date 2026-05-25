using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Inventory.Dtos;
using MediatR;

namespace B2B.Application.Inventory.Commands.Unreserve
{
    public sealed record UnreserveInventoryCommand(
        Guid OrderId,
        IReadOnlyList<InventoryItemDto> Items
    ) : IRequest<InventoryOrderResultDto>;
}
