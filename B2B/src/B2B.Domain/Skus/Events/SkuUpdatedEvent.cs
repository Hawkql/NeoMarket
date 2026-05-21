using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Skus.Events
{
    public sealed record SkuUpdatedEvent(Guid SkuId, Guid ProductId) : DomainEvent;

}
