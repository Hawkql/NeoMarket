using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Orders.Dtos;
using MediatR;

namespace B2C.Application.Orders.Queries.GetMyOrder
{
    public sealed record GetMyOrderQuery(Guid OrderId) : IRequest<OrderDetailDto>;
}
