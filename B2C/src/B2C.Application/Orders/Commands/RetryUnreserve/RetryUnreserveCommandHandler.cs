using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common;
using B2C.Application.Integration.Dtos;
using B2C.Domain.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.Orders.Commands.RetryUnreserve
{
    public sealed class RetryUnreserveCommandHandler : IRequestHandler<RetryUnreserveCommand>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IB2BReservationClient _b2bReservation;
        private readonly ILogger<RetryUnreserveCommandHandler> _logger;

        public RetryUnreserveCommandHandler(
            IOrderRepository orderRepository,
            IB2BReservationClient b2bReservation,
            ILogger<RetryUnreserveCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _b2bReservation = b2bReservation;
            _logger = logger;
        }

        public async Task Handle(RetryUnreserveCommand request, CancellationToken ct)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);
            if (order is null || order.Status != OrderStatus.CancelPending)
                return;  // заказ исчез или уже не в CANCEL_PENDING — нечего делать

            var unreserveKey = IdempotencyKeyFactory.FromParts("unreserve", order.Id.ToString());
            var lines = order.Items.Select(i => new ReserveLine(i.SkuId, i.Quantity)).ToList();

            bool ok;
            try
            {
                ok = await _b2bReservation.UnreserveAsync(unreserveKey, lines, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Retry unreserve failed for order {OrderId}", order.Id);
                ok = false;
            }

            if (ok)
            {
                order.CompleteCancelAfterRetry();
                _logger.LogInformation("Order {OrderId} CANCEL_PENDING → CANCELLED (retry OK)", order.Id);
            }
            else
            {
                order.RecordUnreserveAttempt();
                _logger.LogWarning("Order {OrderId} still CANCEL_PENDING (retry failed)", order.Id);
            }
        }
    }
}
