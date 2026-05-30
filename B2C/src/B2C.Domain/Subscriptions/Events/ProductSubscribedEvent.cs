using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Subscriptions.Events
{
    public record ProductSubscribedEvent(
        Guid SubscriptionId,
        Guid BuyerId,
        Guid ProductId,
        NotifyOn NotifyOn) : DomainEvent;
}
