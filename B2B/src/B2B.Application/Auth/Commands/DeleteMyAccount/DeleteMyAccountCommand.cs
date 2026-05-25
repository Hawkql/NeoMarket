using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Application.Auth.Commands.DeleteMyAccount
{
    public sealed record DeleteMyAccountCommand(Guid SellerId) : IRequest;
}
