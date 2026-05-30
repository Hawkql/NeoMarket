using System.Collections.Generic;
using B2C.Application.IntegrationEvents;
using B2C.Domain.Common;

namespace B2C.Application.Common.Abstractions
{
    public interface IIntegrationEventMapper
    {
        IEnumerable<MappedIntegrationEvent> Map(DomainEvent domainEvent);
    }

    /// <summary>
    /// Результат маппинга. Destination — символьное имя сервиса-получателя ("b2b"),
    /// конкретный URL/auth — забота диспетчера в Infrastructure.
    /// </summary>
    public sealed record MappedIntegrationEvent(
        IntegrationEvent Event,
        string Destination,
        string EventType);
}
