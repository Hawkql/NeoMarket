using B2C.Api.Contracts;
using B2C.Application.IntegrationEvents.Inbound.Commands.HandlePriceChanged;
using B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductBlocked;
using B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductDeleted;
using B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductHardBlocked;
using B2C.Application.IntegrationEvents.Inbound.Commands.HandleSkuBackInStock;
using B2C.Application.IntegrationEvents.Inbound.Commands.HandleSkuOutOfStock;
using B2C.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// Приём межсервисных событий от B2B (US-ORD-04 / EVT-3).
    /// 
    /// INBOUND server-to-server: B2B вызывает нас. Аутентификация — X-Service-Key
    /// (политика ServiceOnly), НЕ JWT покупателя. Это per-service auth.
    /// 
    /// Один endpoint принимает 6 типов события, маршрутизация по event_type
    /// (паттерн Discriminated Routing). Это вариант Strategy Pattern: каждый event_type
    /// → отдельный handler с собственной бизнес-логикой и идемпотентностью через Inbox.
    /// 
    /// openapi: POST /api/v1/b2b/events → 202 Accepted, idempotency_key TTL 24h.
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
            // event_type приходит UPPER_SNAKE_CASE по openapi. ToUpperInvariant + Trim
            // — устойчивы к мелким расхождениям регистра/пробелов между сервисами.
            var eventType = (envelope.EventType ?? string.Empty)
                .Trim().ToUpperInvariant();

            switch (eventType)
            {
                case "PRODUCT_BLOCKED":
                    RequireProductId(envelope, eventType);
                    await _mediator.Send(new HandleProductBlockedCommand(
                        envelope.IdempotencyKey,
                        envelope.Payload!.ProductId!.Value,
                        Array.Empty<Guid>()), ct);
                    break;

                case "PRODUCT_HARD_BLOCKED":
                    RequireProductId(envelope, eventType);
                    await _mediator.Send(new HandleProductHardBlockedCommand(
                        envelope.IdempotencyKey,
                        envelope.Payload!.ProductId!.Value,
                        envelope.Payload.Reason), ct);
                    break;

                case "PRODUCT_DELETED":
                    RequireProductId(envelope, eventType);
                    await _mediator.Send(new HandleProductDeletedCommand(
                        envelope.IdempotencyKey,
                        envelope.Payload!.ProductId!.Value,
                        Array.Empty<Guid>()), ct);
                    break;

                case "SKU_OUT_OF_STOCK":
                    RequireSkuStock(envelope, eventType);
                    await _mediator.Send(new HandleSkuOutOfStockCommand(
                        envelope.IdempotencyKey,
                        envelope.Payload!.ProductId!.Value,
                        envelope.Payload.SkuId!.Value), ct);
                    break;

                case "SKU_BACK_IN_STOCK":
                    RequireSkuStock(envelope, eventType);
                    await _mediator.Send(new HandleSkuBackInStockCommand(
                        envelope.IdempotencyKey,
                        envelope.Payload!.ProductId!.Value,
                        envelope.Payload.SkuId!.Value,
                        envelope.Payload.AvailableQuantity ?? 0), ct);
                    break;

                case "PRICE_CHANGED":
                    RequirePriceChanged(envelope, eventType);
                    await _mediator.Send(new HandlePriceChangedCommand(
                        envelope.IdempotencyKey,
                        envelope.Payload!.ProductId!.Value,
                        envelope.Payload.SkuId!.Value,
                        envelope.Payload.OldPrice!.Value,
                        envelope.Payload.NewPrice!.Value), ct);
                    break;

                default:
                    throw new DomainException(
                        $"Unknown event type: {envelope.EventType}", "INVALID_REQUEST");
            }

            // openapi: 202 Accepted на успешный приём (события обрабатываются асинхронно
            // в рамках транзакции запроса; формально мы принимаем к обработке).
            return Accepted();
        }

        // ===== payload-валидация по типу события =====

        private static void RequireProductId(B2BEventEnvelope envelope, string eventType)
        {
            if (envelope.Payload?.ProductId is null || envelope.Payload.ProductId.Value == Guid.Empty)
                throw new DomainException(
                    $"payload.product_id is required for {eventType}", "INVALID_REQUEST");
        }

        private static void RequireSkuStock(B2BEventEnvelope envelope, string eventType)
        {
            if (envelope.Payload?.ProductId is null || envelope.Payload.ProductId.Value == Guid.Empty
                || envelope.Payload.SkuId is null || envelope.Payload.SkuId.Value == Guid.Empty)
                throw new DomainException(
                    $"payload.product_id and payload.sku_id are required for {eventType}",
                    "INVALID_REQUEST");
        }

        private static void RequirePriceChanged(B2BEventEnvelope envelope, string eventType)
        {
            if (envelope.Payload?.ProductId is null || envelope.Payload.ProductId.Value == Guid.Empty
                || envelope.Payload.SkuId is null || envelope.Payload.SkuId.Value == Guid.Empty
                || envelope.Payload.OldPrice is null
                || envelope.Payload.NewPrice is null)
                throw new DomainException(
                    $"payload.product_id, sku_id, old_price, new_price are required for {eventType}",
                    "INVALID_REQUEST");
        }
    }
}