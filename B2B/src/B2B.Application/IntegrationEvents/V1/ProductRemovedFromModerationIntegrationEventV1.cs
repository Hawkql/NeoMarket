using B2B.Application.IntegrationEvents;

namespace B2B.Application.IntegrationEvents.V1
{
    /// <summary>
    /// Уходит в Moderation как product.removed_from_moderation.v1.
    /// Сообщает: товар вернулся в CREATED, открытую заявку на модерацию можно закрыть.
    /// </summary
    public sealed record ProductRemovedFromModerationIntegrationEventV1(DateTime OccurredOnUtc, Guid ProductId, Guid SellerId) : IntegrationEvent;
}