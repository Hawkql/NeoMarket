using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Invoices.Events
{
    public sealed record InvoiceAcceptedLine(Guid SkuId, int AcceptedQuantity);
    /// <summary>
    /// Накладная принята (полностью или частично).
    /// Это событие триггерит integration event на изменение остатков.
    /// </summary>
    public sealed record InvoiceAcceptedEvent(
     Guid InvoiceId,
     Guid SellerId,
     InvoiceStatus FinalStatus,
     IReadOnlyCollection<InvoiceAcceptedLine> AcceptedLines) : DomainEvent;
}
