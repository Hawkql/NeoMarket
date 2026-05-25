using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Auth.Dtos;
using MediatR;

namespace B2B.Application.Auth.Commands.Refresh
{
    public sealed record RefreshCommand(string RefreshToken)
    : IRequest<TokenResponseDto>;
}
