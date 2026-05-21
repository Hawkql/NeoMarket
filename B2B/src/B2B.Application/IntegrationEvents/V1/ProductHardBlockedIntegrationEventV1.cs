using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.IntegrationEvents.V1
{
    public sealed record ProductHardBlockedIntegrationEventV1 : IntegrationEvent
    {
        public required Guid ProductId { get; init; }
        public required Guid[] SkuIds { get; init; }
    }
}
