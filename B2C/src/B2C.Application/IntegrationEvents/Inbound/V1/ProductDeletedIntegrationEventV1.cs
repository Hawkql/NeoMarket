using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.IntegrationEvents.Inbound.V1
{
    /// <summary>
    /// EVT-3 от B2B: товар окончательно удалён продавцом.
    /// 
    /// Реакция B2C (US-ORD-04):
    ///   - cart_items: UnavailableReason = ProductDeleted;
    ///   - favorites: удалить (товара больше нет);
    ///   - orders: НЕ трогаются.
    /// </summary>
    public sealed record ProductDeletedIntegrationEventV1 : IntegrationEvent
    {
        public required Guid ProductId { get; init; }
        public required Guid[] SkuIds { get; init; }
    }
}
