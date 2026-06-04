using System;
using System.Collections.Generic;
using B2C.Application.Orders.Dtos;
using MediatR;

namespace B2C.Application.Orders.Commands.CreateOrder
{
    public sealed record CreateOrderCommand(
        Guid IdempotencyKey,
        Guid AddressId,
        Guid PaymentMethodId,
        string? Comment,
        IReadOnlyList<CreateOrderItem> Items) : IRequest<OrderResponseDto>;

    public sealed record CreateOrderItem(Guid SkuId, int Quantity);
}