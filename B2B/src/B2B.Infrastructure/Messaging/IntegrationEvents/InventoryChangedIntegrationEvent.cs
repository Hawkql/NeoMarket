using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.Messaging.IntegrationEvents
{
    internal record InventoryChangedIntegrationEvent(
    Guid InvoiceId,
    Guid SellerId,
    IReadOnlyList<InventoryChangedLine> Lines,
    DateTime OccurredOn);

    internal record InventoryChangedLine(Guid SkuId, int Quantity);
}
