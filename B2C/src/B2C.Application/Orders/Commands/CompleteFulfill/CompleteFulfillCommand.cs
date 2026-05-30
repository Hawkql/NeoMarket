using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Orders.Commands.CompleteFulfill
{
    public sealed record CompleteFulfillCommand(Guid OrderId) : IRequest;
}
