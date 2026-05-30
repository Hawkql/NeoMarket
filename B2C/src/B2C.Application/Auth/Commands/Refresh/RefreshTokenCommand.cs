using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Auth.Dtos;
using MediatR;

namespace B2C.Application.Auth.Commands.Refresh
{
    public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<TokenResponseDto>;
}
