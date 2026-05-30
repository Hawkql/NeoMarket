using B2C.Api.Contracts;
using B2C.Application.Subscriptions.Commands.Subscribe;
using B2C.Application.Subscriptions.Commands.Unsubscribe;
using B2C.Application.Subscriptions.Commands.UpdateSubscription;
using B2C.Application.Subscriptions.Dtos;
using B2C.Application.Subscriptions.Queries.ListMySubscriptions;
using B2C.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// Подписки на товар (US-CART-02). Требует авторизации.
    /// </summary>
    [ApiController]
    [Route("api/v1/subscriptions")]
    [Authorize]
    public sealed class SubscriptionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SubscriptionsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> ListSubscriptions(CancellationToken ct)
        {
            var subscriptions = await _mediator.Send(new ListMySubscriptionsQuery(), ct);
            return Ok(subscriptions);
        }

        [HttpPost("{productId:guid}")]
        public async Task<IActionResult> Subscribe(
            Guid productId, [FromBody] SubscribeRequest request, CancellationToken ct)
        {
            await _mediator.Send(new SubscribeCommand(
                productId, ParseNotifyOn(request.NotifyOn)), ct);
            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpPatch("{productId:guid}")]
        public async Task<IActionResult> UpdateSubscription(
            Guid productId, [FromBody] UpdateSubscriptionRequest request, CancellationToken ct)
        {
            await _mediator.Send(new UpdateSubscriptionCommand(
                productId, ParseNotifyOn(request.NotifyOn)), ct);
            return NoContent();
        }

        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> Unsubscribe(Guid productId, CancellationToken ct)
        {
            await _mediator.Send(new UnsubscribeCommand(productId), ct);
            return NoContent();
        }

        /// <summary>
        /// Собирает массив строк ["in_stock", "price_drop"] в flags-enum NotifyOnDto.
        /// Неизвестное значение → 400.
        /// </summary>
        private static NotifyOnDto ParseNotifyOn(string[] values)
        {
            if (values is null || values.Length == 0)
                throw new DomainException("notify_on must not be empty", "INVALID_REQUEST");

            var result = NotifyOnDto.None;
            foreach (var v in values)
            {
                result |= v.ToLowerInvariant() switch
                {
                    "in_stock" => NotifyOnDto.InStock,
                    "price_drop" => NotifyOnDto.PriceDrop,
                    _ => throw new DomainException(
                        $"Unknown notify_on value: {v}", "INVALID_REQUEST"),
                };
            }
            return result;
        }
    }
}
