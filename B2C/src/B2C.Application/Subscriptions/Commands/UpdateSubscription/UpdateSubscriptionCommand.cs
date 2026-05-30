using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Subscriptions.Dtos;
using MediatR;

namespace B2C.Application.Subscriptions.Commands.UpdateSubscription
{
    public sealed record UpdateSubscriptionCommand(
        Guid ProductId,
        NotifyOnDto NotifyOn) : IRequest;
}
