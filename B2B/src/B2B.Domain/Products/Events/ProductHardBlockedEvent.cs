using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Products.Events
{
    public sealed record ProductHardBlockedEvent(
    Guid ProductId,
    IReadOnlyCollection<Guid> SkuIds) : DomainEvent;
}
