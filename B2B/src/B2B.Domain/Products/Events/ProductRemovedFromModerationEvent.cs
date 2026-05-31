using B2B.Domain.Common;

namespace B2B.Domain.Products.Events
{
    /// <summary>
    /// Товар сошёл с модерации, потому что не осталось ни одного живого SKU.
    /// Возникает при удалении последнего SKU у товара в статусе ON_MODERATION
    /// (US-12, требование b2b-flows.md:1536).
    ///
    /// Подписчики:
    ///   - Moderation: закрыть открытую заявку на модерацию этого товара.
    /// </summary>
    public sealed record ProductRemovedFromModerationEvent(Guid ProductId, Guid SellerId) : DomainEvent;
}