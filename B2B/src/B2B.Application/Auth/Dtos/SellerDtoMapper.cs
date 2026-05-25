using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Sellers;

namespace B2B.Application.Auth.Dtos
{
    internal static class SellerDtoMapper
    {
        public static SellerResponseDto Map(Seller s) => new(
            Id: s.Id,
            Email: s.Email,
            FirstName: s.FirstName,
            LastName: s.LastName,
            MiddleName: s.MiddleName,
            CompanyName: s.CompanyName,
            Inn: s.Inn,
            Phone: s.Phone,
            CreatedAt: s.CreatedAt,
            UpdatedAt: s.UpdatedAt);
        
    }
}
