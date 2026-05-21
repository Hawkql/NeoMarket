using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Invoices;

namespace B2B.Application.IntegrationEvents.V1
{
    public sealed record InvoiceAcceptedIntegrationEventV1 : IntegrationEvent
    {
        public required Guid InvoiceId { get; init; }
        public required InvoiceAcceptedLineV1[] AcceptedLines { get; init; }
    }

    public sealed record InvoiceAcceptedLineV1(Guid SkuId, int AcceptedQuantity);
}
