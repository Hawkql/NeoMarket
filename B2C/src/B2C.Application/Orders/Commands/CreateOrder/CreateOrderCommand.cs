using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Orders.Dtos;
using MediatR;

namespace B2C.Application.Orders.Commands.CreateOrder
{
    public sealed record CreateOrderCommand(
        Guid IdempotencyKey,
        string DeliveryAddress,
        IReadOnlyList<CreateOrderItem> Items) : IRequest<OrderDetailDto>;

    public sealed record CreateOrderItem(Guid SkuId, int Quantity);
}
