using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Invoices.Events
{
    public record AcceptedLine(Guid SkuId, int Quantity);
    public record InvoiceAcceptedEvent(Guid InvoiceId,
        Guid SellerId,
        IReadOnlyList<AcceptedLine>Lines):DomainEvent;

}
