using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Orders.Commands.RetryUnreserve
{
    public sealed record RetryUnreserveCommand(Guid OrderId) : IRequest;
}
