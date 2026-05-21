using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Invoices.Events
{
   public sealed record InvoiceCreatedEvent(Guid InvoiceId,Guid SellerId,
       IReadOnlyCollection<Guid> SkuId):DomainEvent;
}
