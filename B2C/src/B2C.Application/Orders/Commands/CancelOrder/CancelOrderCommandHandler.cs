using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common;
using B2C.Application.Common.Abstractions;
using B2C.Application.Integration.Dtos;
using B2C.Application.Orders.Dtos;
using B2C.Domain.Common;
using B2C.Domain.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.Orders.Commands.CancelOrder
{
    public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderDetailDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IB2BReservationClient _b2bReservation;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CancelOrderCommandHandler> _logger;

        public CancelOrderCommandHandler(
            IOrderRepository orderRepository,
            IB2BReservationClient b2bReservation,
            ICurrentUserService currentUser,
            ILogger<CancelOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _b2bReservation = b2bReservation;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<OrderDetailDto> Handle(CancelOrderCommand request, CancellationToken ct)
        {
            var order = await _orderRepository.GetByIdForBuyerAsync(
                request.OrderId, _currentUser.BuyerId, ct)
                ?? throw new DomainException("Order not found", "NOT_FOUND");

            // Domain проверит статус и бросит CONFLICT, если нельзя отменять.
            // Делаем это до обращения к B2B, чтобы не дёргать unreserve зря.
            // (Метод EnsureCancellableStatus приватный — он вызовется внутри MarkAsCancelled/Pending.
            //  Чтобы не делать лишний unreserve на невалидном статусе, добавим публичный guard в Domain.)
            if (!order.CanBeCancelled)
                throw new DomainException(
                    $"Order in status {order.Status} cannot be cancelled",
                    "CANCEL_NOT_ALLOWED",
                    details: new { current_status = order.Status.ToString().ToLowerInvariant() });
            // Детерминированный ключ из order_id — для идемпотентного retry.
            var unreserveKey = IdempotencyKeyFactory.FromParts("unreserve", order.Id.ToString());

            var unreserveLines = order.Items
                .Select(i => new ReserveLine(i.SkuId, i.Quantity))
                .ToList();

            bool unreserveOk;
            try
            {
                unreserveOk = await _b2bReservation.UnreserveAsync(unreserveKey, unreserveLines, ct);
            }
            catch (Exception ex)
            {
                // Сетевая ошибка / B2B недоступен — считаем как fail, уходим в CANCEL_PENDING.
                _logger.LogWarning(ex,
                    "Unreserve call failed for order {OrderId} — moving to CANCEL_PENDING", order.Id);
                unreserveOk = false;
            }

            if (unreserveOk)
            {
                order.MarkAsCancelled();
                _logger.LogInformation("Order {OrderId} cancelled (unreserve OK)", order.Id);
            }
            else
            {
                order.MarkAsCancelPending();
                _logger.LogWarning("Order {OrderId} → CANCEL_PENDING (unreserve failed)", order.Id);
            }

            return OrdersMapper.ToDetailDto(order);
        }
    }
}
