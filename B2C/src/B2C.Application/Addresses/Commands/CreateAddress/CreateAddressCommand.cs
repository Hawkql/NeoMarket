using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Addresses.Dtos;
using MediatR;

namespace B2C.Application.Addresses.Commands.CreateAddress
{
    /// <summary>
    /// Создание адреса. BuyerId НЕ в команде — берётся из ICurrentUserService (IDOR-защита).
    /// </summary>
    public sealed record CreateAddressCommand(
        string Country,
        string City,
        string Street,
        string? House,
        string? Apartment,
        string? PostalCode,
        bool IsDefault) : IRequest<AddressDto>;
}
