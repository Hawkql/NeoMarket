using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Orders.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.Orders.EventHandlers
{
    public sealed class OrderCreatedDomainEventHandler : INotificationHandler<OrderCreatedEvent>
    {
        private readonly ILogger<OrderCreatedDomainEventHandler> _logger;

        public OrderCreatedDomainEventHandler(ILogger<OrderCreatedDomainEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(OrderCreatedEvent notification, CancellationToken ct)
        {
            _logger.LogInformation(
                "Order {OrderId} created for buyer {BuyerId}, total {Total}",
                notification.OrderId, notification.BuyerId, notification.TotalAmount);
            return Task.CompletedTask;
        }
    }
}
