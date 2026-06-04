using System;
using B2C.Application.Orders.Dtos;
using MediatR;

namespace B2C.Application.Orders.Queries.GetMyOrder
{
    public sealed record GetMyOrderQuery(Guid OrderId) : IRequest<OrderResponseDto>;
}