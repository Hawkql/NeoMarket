using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Auth.Commands.ChangePassword
{
    public sealed record ChangePasswordCommand(string NewPassword) : IRequest;
}
