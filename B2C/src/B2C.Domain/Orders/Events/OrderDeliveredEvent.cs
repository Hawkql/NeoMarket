using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Orders.Events
{
    public record OrderDeliveredEvent(Guid OrderId, Guid BuyerId) : DomainEvent;
}
