using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Addresses;

namespace B2C.Application.Addresses.Dtos
{
    /// <summary>Маппинг Address aggregate → AddressDto.</summary>
    internal static class AddressDtoMapper
    {
        public static AddressDto ToDto(Address a) =>
            new(a.Id,
                a.Country,
                a.City,
                a.Street,
                a.House,
                a.Apartment,
                a.PostalCode,
                a.IsDefault,
                a.CreatedAt);
    }
}
