using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.IntegrationEvents.V1
{
    public sealed record ProductSentToModerationIntegrationEventV1 : IntegrationEvent
    {
        public required Guid ProductId { get; init; }
        public required Guid SellerId { get; init; }
        public required string Reason { get; init; }   // "first_sku_added" | "edited"
    }
}
