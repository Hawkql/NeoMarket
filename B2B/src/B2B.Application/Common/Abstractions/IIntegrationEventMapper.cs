using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.IntegrationEvents;
using B2B.Domain.Common;

namespace B2B.Application.Common.Abstractions
{
    public interface IIntegrationEventMapper
    {
        /// <summary>
        /// Маппит Domain Event в коллекцию Integration Events.
        /// </summary>
        /// <returns>Пары (Integration Event + Destination), пусто если не маппится.</returns>
        IEnumerable<MappedIntegrationEvent> Map(DomainEvent domainEvent);
    }
    public sealed record MappedIntegrationEvent(
    IntegrationEvent Event,
    string Destination,
    string EventType);
}
