using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Addresses.Commands.UpdateAddress
{
    /// <summary>
    /// Обновление адреса. AddressId — из URL path, BuyerId — из JWT.
    /// Обрати внимание: IsDefault НЕ передаётся (для смены default — отдельный endpoint
    /// SetDefaultAddress, потому что это другая операция, не "редактирование адреса").
    /// </summary>
    public sealed record UpdateAddressCommand(
        Guid AddressId,
        string Country,
        string City,
        string Street,
        string? House,
        string? Apartment,
        string? PostalCode) : IRequest;
}
