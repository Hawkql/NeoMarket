using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.IntegrationEvents.V1
{
    public sealed record SkuOutOfStockIntegrationEventV1 : IntegrationEvent
    {
        public required Guid SkuId { get; init; }
        public required Guid ProductId { get; init; }
    }
}
