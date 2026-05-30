using B2C.Api.Contracts;
using B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductBlocked;
using B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductDeleted;
using B2C.Application.IntegrationEvents.Inbound.Commands.HandleSkuOutOfStock;
using B2C.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// Приём межсервисных событий ОТ B2B (US-ORD-04 / EVT-3).
    /// INBOUND: B2B вызывает нас. Аутентификация — X-Service-Key (политика ServiceOnly),
    /// НЕ JWT покупателя. Это per-service auth для server-to-server.
    /// 
    /// Один endpoint принимает все три типа события, маршрутизация по EventType
    /// (discriminator) — как B2B принимал от Moderation один endpoint с status-дискриминатором.
    /// </summary>
    [ApiController]
    [Route("api/v1/b2b/events")]
    [Authorize(Policy = "ServiceOnly")]
    public sealed class B2BEventsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public B2BEventsController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        public async Task<IActionResult> Receive(
            [FromBody] B2BEventEnvelope envelope, CancellationToken ct)
        {
            switch (envelope.EventType?.ToLowerInvariant())
            {
                case "product_blocked":
                    await _mediator.Send(new HandleProductBlockedCommand(
                        envelope.IdempotencyKey,
                        envelope.Data.ProductId,
                        envelope.Data.SkuIds ?? Array.Empty<Guid>()), ct);
                    break;

                case "product_deleted":
                    await _mediator.Send(new HandleProductDeletedCommand(
                        envelope.IdempotencyKey,
                        envelope.Data.ProductId,
                        envelope.Data.SkuIds ?? Array.Empty<Guid>()), ct);
                    break;

                case "sku_out_of_stock":
                    if (envelope.Data.SkuId is null)
                        throw new DomainException("sku_id is required for sku_out_of_stock", "INVALID_REQUEST");

                    await _mediator.Send(new HandleSkuOutOfStockCommand(
                        envelope.IdempotencyKey,
                        envelope.Data.ProductId,
                        envelope.Data.SkuId.Value), ct);
                    break;

                default:
                    throw new DomainException(
                        $"Unknown event type: {envelope.EventType}", "INVALID_REQUEST");
            }

            // 200 с ok=true — B2B-сторона ожидает подтверждение приёма.
            return Ok(new { ok = true });
        }
    }
}
