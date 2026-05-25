using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Auth.Dtos;
using MediatR;

namespace B2B.Application.Auth.Commands.UpdateMyProfile
{
    public sealed record UpdateMyProfileCommand(
        Guid SellerId,
        string? FirstName,
        string? LastName,
        string? MiddleName,
        string? CompanyName,
        string? Phone
    ) : IRequest<SellerResponseDto>;
}
