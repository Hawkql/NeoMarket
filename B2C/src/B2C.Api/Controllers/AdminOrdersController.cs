using B2C.Api.Contracts;
using B2C.Application.Orders.Commands.TransitionOrderStatus;
using B2C.Application.Orders.Dtos;
using B2C.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// Админ/операторские операции с заказами (ADM-B2C-1): смена статуса
    /// PAID→ASSEMBLING→DELIVERING→DELIVERED.
    /// 
    /// Защита ServiceOnly (X-Service-Key) — это операции внутренней админки/оператора склада,
    /// не покупателя. Здесь нет IDOR-фильтра по BuyerId: оператор управляет любым заказом.
    /// 
    /// При переходе в DELIVERED поднимется OrderDeliveredEvent → fulfill в B2B (US-ORD-05).
    /// </summary>
    [ApiController]
    [Route("api/v1/admin/orders")]
    [Authorize(Policy = "ServiceOnly")]
    public sealed class AdminOrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminOrdersController(IMediator mediator) => _mediator = mediator;

        [HttpPost("{orderId:guid}/transition")]
        public async Task<IActionResult> Transition(
            Guid orderId, [FromBody] TransitionOrderStatusRequest request, CancellationToken ct)
        {
            var order = await _mediator.Send(new TransitionOrderStatusCommand(
                orderId, ParseTargetStatus(request.TargetStatus)), ct);

            return Ok(order);
        }

        private static OrderStatusDto ParseTargetStatus(string status) => status.ToLowerInvariant() switch
        {
            "assembling" => OrderStatusDto.Assembling,
            "delivering" => OrderStatusDto.Delivering,
            "delivered" => OrderStatusDto.Delivered,
            _ => throw new DomainException(
                $"Unsupported transition target: {status}. " +
                "Allowed: assembling, delivering, delivered.", "INVALID_REQUEST"),
        };
    }
}
