using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Subscriptions.Commands.Unsubscribe
{
    public sealed record UnsubscribeCommand(Guid ProductId) : IRequest;
}
