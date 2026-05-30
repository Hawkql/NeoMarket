using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Orders.Dtos;
using MediatR;

namespace B2C.Application.Orders.Commands.TransitionOrderStatus
{
    public sealed record TransitionOrderStatusCommand(
       Guid OrderId,
       OrderStatusDto TargetStatus) : IRequest<OrderDetailDto>;
}
