using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Auth.Dtos
{
    public sealed record SellerResponseDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string? MiddleName,
        string CompanyName,
        string Inn,
        string? Phone,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
