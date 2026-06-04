using System;
using System.Collections.Generic;

namespace B2C.Application.Cart.Dtos
{
    /// <summary>
    /// openapi: CartResponse. required: items, items_count, subtotal, is_valid.
    /// Optional: id, updated_at.
    /// 
    /// is_valid = true ↔ все позиции is_available AND quantity ≤ available_quantity.
    /// subtotal = сумма line_total по AVAILABLE items (недоступные не учитываются).
    /// items_count = сумма quantity по доступным позициям.
    /// </summary>
    public sealed record CartDto(
        Guid Id,
        IReadOnlyList<CartItemDto> Items,
        int ItemsCount,
        int Subtotal,
        bool IsValid,
        DateTime UpdatedAt);
}