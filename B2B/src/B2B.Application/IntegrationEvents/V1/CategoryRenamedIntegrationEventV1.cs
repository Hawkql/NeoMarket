using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.IntegrationEvents.V1
{
    public sealed record CategoryRenamedIntegrationEventV1 : IntegrationEvent
    {
        public required Guid CategoryId { get; init; }
        public required string NewName { get; init; }
    }
}
