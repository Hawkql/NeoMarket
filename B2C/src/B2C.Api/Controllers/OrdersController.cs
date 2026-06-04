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
        /// <summary>
        /// openapi: POST /api/v1/orders. Header Idempotency-Key обязателен (TTL 1 час).
        /// IDOR: AddressId/PaymentMethodId проверяются в handler — чужие → NOT_FOUND.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder(
            [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
            [FromBody] CreateOrderRequest request,
            CancellationToken ct)
        {
            var items = request.Items
                .Select(i => new CreateOrderItem(i.SkuId, i.Quantity))
                .ToList();

            var order = await _mediator.Send(new CreateOrderCommand(
                idempotencyKey,
                request.AddressId,
                request.PaymentMethodId,
                request.Comment,
                items), ct);

            return CreatedAtAction(nameof(GetOrder), new { order_id = order.Id }, order);
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
        [HttpGet("{order_id:guid}")]
        public async Task<IActionResult> GetOrder(
             [FromRoute(Name = "order_id")] Guid orderId,
             CancellationToken ct)
        {
            var order = await _mediator.Send(new GetMyOrderQuery(orderId), ct);
            return Ok(order);
        }

        /// <summary>
        /// openapi: POST /orders/{order_id}/cancel. requestBody optional с reason ≤ 500.
        /// Ответ 200 OrderResponse — содержит свежий статус (CANCELLED либо CANCEL_PENDING).
        /// </summary>
        [HttpPost("{order_id:guid}/cancel")]
        public async Task<IActionResult> CancelOrder(
            [FromRoute(Name = "order_id")] Guid orderId,
            [FromBody] CancelOrderRequest? request,
            CancellationToken ct)
        {
            var order = await _mediator.Send(
                new CancelOrderCommand(orderId, request?.Reason), ct);
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
