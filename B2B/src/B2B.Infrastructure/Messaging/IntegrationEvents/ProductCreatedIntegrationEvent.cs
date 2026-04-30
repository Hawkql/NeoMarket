using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.Messaging.IntegrationEvents
{
    internal record ProductCreatedIntegrationEvent(
        Guid ProductId,
        Guid SellerId,
        DateTime OccurredOn);

}
