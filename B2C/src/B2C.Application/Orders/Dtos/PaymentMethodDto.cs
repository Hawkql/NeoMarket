using System;

namespace B2C.Application.Orders.Dtos
{

    public sealed record PaymentMethodDto(
        Guid Id,
        string Type,
        DateTime CreatedAt);
}