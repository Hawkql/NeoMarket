using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Orders.Dtos;
using MediatR;

namespace B2C.Application.Orders.Commands.CancelOrder
{
    public sealed record CancelOrderCommand(Guid OrderId) : IRequest<OrderDetailDto>;
}
