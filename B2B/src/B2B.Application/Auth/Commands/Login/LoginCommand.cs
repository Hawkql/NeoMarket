using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Auth.Dtos;
using MediatR;

namespace B2B.Application.Auth.Commands.Login
{
    public sealed record LoginCommand(string Email, string Password)
    : IRequest<TokenResponseDto>;
}
