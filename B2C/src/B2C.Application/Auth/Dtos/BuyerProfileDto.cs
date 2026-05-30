using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Auth.Dtos
{
    /// <summary>Профиль покупателя. Возвращается на GET/PUT /buyers/me.</summary>
    public sealed record BuyerProfileDto(
        Guid Id,
        string Email,
        string? FirstName,
        string? LastName,
        string? Phone,
        DateTime CreatedAt);
}
