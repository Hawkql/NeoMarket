using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Subscriptions.Dtos;
using MediatR;

namespace B2C.Application.Subscriptions.Queries.ListMySubscriptions
{
    public sealed record ListMySubscriptionsQuery : IRequest<IReadOnlyList<SubscriptionDto>>;
}
