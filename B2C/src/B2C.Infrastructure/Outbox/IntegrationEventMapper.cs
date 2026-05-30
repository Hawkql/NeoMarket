using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Common;

namespace B2C.Infrastructure.Outbox
{
    public sealed class IntegrationEventMapper : IIntegrationEventMapper
    {
        public IEnumerable<MappedIntegrationEvent> Map(DomainEvent domainEvent)
        {
            // B2C ничего не публикует наружу асинхронно — все B2B-вызовы синхронны.
            yield break;
        }
    }
}
