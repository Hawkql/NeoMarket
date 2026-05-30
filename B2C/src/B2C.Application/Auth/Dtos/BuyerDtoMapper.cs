using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Buyers;

namespace B2C.Application.Auth.Dtos
{
    internal static class BuyerDtoMapper
    {
        public static BuyerProfileDto ToProfileDto(Buyer b) =>
            new(b.Id,
                b.Email,
                b.FirstName,
                b.LastName,
                b.Phone,
                b.CreatedAt);
    }
}
