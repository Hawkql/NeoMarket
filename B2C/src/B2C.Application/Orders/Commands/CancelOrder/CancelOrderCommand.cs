using System;
using B2C.Application.Orders.Dtos;
using MediatR;

namespace B2C.Application.Orders.Commands.CancelOrder
{
    /// <summary>
    /// openapi: POST /orders/{order_id}/cancel — body optional с reason ≤ 500.
    /// Reason пробрасываем в Domain (Order.MarkAsCancelled/MarkAsCancelPending).
    /// </summary>
    public sealed record CancelOrderCommand(Guid OrderId, string? Reason = null)
        : IRequest<OrderResponseDto>;
}