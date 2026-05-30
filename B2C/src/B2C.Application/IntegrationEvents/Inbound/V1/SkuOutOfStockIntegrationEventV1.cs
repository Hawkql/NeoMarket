using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.IntegrationEvents.Inbound.V1
{
    /// <summary>
    /// EVT-3 от B2B: у SKU закончились остатки (active_quantity = 0).
    /// 
    /// Реакция B2C (US-ORD-04):
    ///   - cart_items с этим sku_id: UnavailableReason = OutOfStock;
    ///   - favorites: оставляем (покупатель ждёт — это нормальный кейс,
    ///     для этого есть Subscription с notify_on=IN_STOCK);
    ///   - orders: НЕ трогаются.
    /// </summary>
    public sealed record SkuOutOfStockIntegrationEventV1 : IntegrationEvent
    {
        public required Guid ProductId { get; init; }
        public required Guid SkuId { get; init; }
    }
}
