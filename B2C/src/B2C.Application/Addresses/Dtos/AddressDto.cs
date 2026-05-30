using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Addresses.Dtos
{
    /// <summary>Адрес доставки в ответе API.</summary>
    public sealed record AddressDto(
        Guid Id,
        string Country,
        string City,
        string Street,
        string? House,
        string? Apartment,
        string? PostalCode,
        bool IsDefault,
        DateTime CreatedAt);
}
