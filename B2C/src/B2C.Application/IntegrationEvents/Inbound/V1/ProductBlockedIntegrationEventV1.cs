using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.IntegrationEvents.Inbound.V1
{
    /// <summary>
    /// EVT-3 от B2B: товар заблокирован модерацией (BLOCKED или HARD_BLOCKED).
    /// 
    /// Реакция B2C (US-ORD-04):
    ///   - cart_items с этим product_id: UnavailableReason = ProductBlocked;
    ///   - favorites: оставляем (покупатель сам решит — пусть видит "недоступно" на карточке);
    ///   - orders: НЕ трогаются (уже оплаченные заказы обязательны к отгрузке).
    /// </summary>
    public sealed record ProductBlockedIntegrationEventV1 : IntegrationEvent
    {
        public required Guid ProductId { get; init; }
        public required Guid[] SkuIds { get; init; }
    }
}
