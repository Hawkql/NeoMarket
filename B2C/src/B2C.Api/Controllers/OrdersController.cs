using B2C.Api.Contracts;
using B2C.Application.Orders.Commands.CancelOrder;
using B2C.Application.Orders.Commands.CreateOrder;
using B2C.Application.Orders.Dtos;
using B2C.Application.Orders.Queries.GetMyOrder;
using B2C.Application.Orders.Queries.ListMyOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// Заказы покупателя (US-ORD-01..03, 05). Требует авторизации.
    /// BuyerId всегда из JWT (IDOR) — в командах/запросах не передаётся.
    /// </summary>
    [ApiController]
    [Route("api/v1/orders")]
    [Authorize]
    public sealed class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// US-ORD-01: checkout. Возвращает 201 с созданным заказом (status=PAID).
        /// Идемпотентность по idempotency_key: повторный submit вернёт существующий заказ.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder(
            [FromBody] CreateOrderRequest request, CancellationToken ct)
        {
            var items = request.Items
                .Select(i => new CreateOrderItem(i.SkuId, i.Quantity))
                .ToList();

            var order = await _mediator.Send(new CreateOrderCommand(
                request.IdempotencyKey,
                request.DeliveryAddress,
                items), ct);

            return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
        }

        /// <summary>US-ORD-02: список заказов покупателя с пагинацией и фильтром статуса.</summary>
        [HttpGet]
        public async Task<IActionResult> ListOrders(
            [FromQuery] string? status,
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0,
            CancellationToken ct = default)
        {
            var statusFilter = status is null ? null : ParseStatus(status);

            var orders = await _mediator.Send(new ListMyOrdersQuery(statusFilter, limit, offset), ct);
            return Ok(orders);
        }

        /// <summary>US-ORD-02: детали заказа. IDOR-safe — чужой заказ → 404.</summary>
        [HttpGet("{orderId:guid}")]
        public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken ct)
        {
            var order = await _mediator.Send(new GetMyOrderQuery(orderId), ct);
            return Ok(order);
        }

        /// <summary>
        /// US-ORD-03: отмена заказа. unreserve OK → CANCELLED, fail → CANCEL_PENDING.
        /// Отмена ASSEMBLING/DELIVERING/DELIVERED → 409 (Domain CanBeCancelled).
        /// </summary>
        [HttpPost("{orderId:guid}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid orderId, CancellationToken ct)
        {
            var order = await _mediator.Send(new CancelOrderCommand(orderId), ct);
            return Ok(order);
        }

        /// <summary>Парсит строковый статус из query в OrderStatusDto. Неизвестный → null (без фильтра).</summary>
        private static OrderStatusDto? ParseStatus(string status) => status.ToLowerInvariant() switch
        {
            "created" => OrderStatusDto.Created,
            "paid" => OrderStatusDto.Paid,
            "assembling" => OrderStatusDto.Assembling,
            "delivering" => OrderStatusDto.Delivering,
            "delivered" => OrderStatusDto.Delivered,
            "cancelled" => OrderStatusDto.Cancelled,
            "cancel_pending" => OrderStatusDto.CancelPending,
            _ => null,
        };
    }
}
