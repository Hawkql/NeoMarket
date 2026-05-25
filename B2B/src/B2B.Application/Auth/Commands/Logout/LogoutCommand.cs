using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Application.Auth.Commands.Logout
{
    public sealed record LogoutCommand(string RefreshToken) : IRequest;
}
