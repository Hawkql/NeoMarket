using System;
using System.Collections.Generic;

namespace B2C.Application.Orders.Dtos
{
    /// <summary>
    /// openapi: OrderResponse. required:
    ///   id, buyer_id, status, items, subtotal, total, address, created_at.
    /// optional:
    ///   number, status_history, delivery_cost, payment_method,
    ///   comment, cancel_reason, paid_at, delivered_at.
    /// 
    /// Один тип на list и detail — openapi не различает.
    /// status: string, enum [CREATED, PAID, ASSEMBLING, DELIVERING, DELIVERED, CANCELLED, CANCEL_PENDING].
    /// </summary>
    public sealed record OrderResponseDto(
        Guid Id,
        string? Number,
        Guid BuyerId,
        string Status,
        IReadOnlyList<OrderStatusHistoryDto>? StatusHistory,
        IReadOnlyList<OrderItemDto> Items,
        int Subtotal,
        int DeliveryCost,
        int Total,
        AddressDto Address,
        PaymentMethodDto? PaymentMethod,
        string? Comment,
        string? CancelReason,
        DateTime CreatedAt,
        DateTime? PaidAt,
        DateTime? DeliveredAt);

    /// <summary>
    /// openapi: OrderResponse.status_history[i]. На MVP не заполняется (Domain
    /// не ведёт историю отдельной таблицей) — отдаётся как null или пустой массив.
    /// </summary>
    public sealed record OrderStatusHistoryDto(
        string Status,
        DateTime ChangedAt,
        string? Reason);
}