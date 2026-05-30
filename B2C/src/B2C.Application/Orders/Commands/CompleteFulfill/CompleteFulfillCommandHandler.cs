using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration.Dtos;
using B2C.Domain.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.Orders.Commands.CompleteFulfill
{
    public sealed class CompleteFulfillCommandHandler : IRequestHandler<CompleteFulfillCommand>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IB2BReservationClient _b2bReservation;
        private readonly ILogger<CompleteFulfillCommandHandler> _logger;

        public CompleteFulfillCommandHandler(
            IOrderRepository orderRepository,
            IB2BReservationClient b2bReservation,
            ILogger<CompleteFulfillCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _b2bReservation = b2bReservation;
            _logger = logger;
        }

        public async Task Handle(CompleteFulfillCommand request, CancellationToken ct)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);
            if (order is null || !order.RequiresFulfill)
                return;  // нечего делать (no-op идемпотентность)

            var lines = order.Items.Select(i => new ReserveLine(i.SkuId, i.Quantity)).ToList();

            var ok = await _b2bReservation.FulfillAsync(order.Id, lines, ct);
            if (!ok)
            {
                _logger.LogWarning(
                    "Fulfill failed for order {OrderId} — will retry async", order.Id);
                return;  // не помечаем completed, background-job повторит
            }

            order.MarkFulfillCompleted();
            _logger.LogInformation("Order {OrderId} fulfill completed", order.Id);
        }
    }
}
