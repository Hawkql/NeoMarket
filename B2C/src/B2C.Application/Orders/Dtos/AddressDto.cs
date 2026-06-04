using System;

namespace B2C.Application.Orders.Dtos
{

    public sealed record AddressDto(
        Guid Id,
        string Country,
        string City,
        string Street,
        string? House,
        string? Apartment,
        string? PostalCode,
        DateTime CreatedAt);
}