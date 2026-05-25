using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Auth.Dtos;
using MediatR;

namespace B2B.Application.Auth.Commands.Register
{
    public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? MiddleName,
    string CompanyName,
    string Inn,
    string? Phone
) : IRequest<TokenResponseDto>;
}
