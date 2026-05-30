using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Orders.Commands.CompleteFulfill;
using B2C.Domain.Orders.Events;
using MediatR;

namespace B2C.Application.Orders.EventHandlers
{
    public sealed class OrderDeliveredDomainEventHandler : INotificationHandler<OrderDeliveredEvent>
    {
        private readonly IMediator _mediator;

        public OrderDeliveredDomainEventHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(OrderDeliveredEvent notification, CancellationToken ct)
        {
            await _mediator.Send(new CompleteFulfillCommand(notification.OrderId), ct);
        }
    }
}
