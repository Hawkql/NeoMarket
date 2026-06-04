using System;

namespace B2C.Application.Orders.Dtos
{

    public sealed record OrderItemDto(
        Guid SkuId,
        Guid ProductId,
        string Name,
        string? SkuCode,
        int Quantity,
        int UnitPrice,
        int LineTotal);
}