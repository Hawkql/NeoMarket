using B2C.Api.Contracts;
using B2C.Application.Favorites.Commands.AddFavorite;
using B2C.Application.Favorites.Commands.RemoveFavorite;
using B2C.Application.Favorites.Queries.ListMyFavorites;
using B2C.Application.Subscriptions.Commands.Subscribe;
using B2C.Application.Subscriptions.Commands.Unsubscribe;
using B2C.Application.Subscriptions.Dtos;
using B2C.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// Избранное (US-CART-01). Требует авторизации — избранное привязано к покупателю.
    /// </summary>
    [ApiController]
    [Route("api/v1/favorites")]
    [Authorize]
    public sealed class FavoritesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public FavoritesController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// openapi: GET /api/v1/favorites → PaginatedCatalogProducts.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ListFavorites(
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0,
            CancellationToken ct = default)
        {
            var favorites = await _mediator.Send(new ListMyFavoritesQuery(limit, offset), ct);
            return Ok(favorites);
        }

        /// <summary>
        /// openapi: PUT /api/v1/favorites/{product_id} → 204 (идемпотентно).
        /// Параметр пути в snake_case (product_id) согласно openapi.
        /// </summary>
        [HttpPut("{product_id:guid}")]
        public async Task<IActionResult> AddFavorite(
            [FromRoute(Name = "product_id")] Guid productId, CancellationToken ct)
        {
            await _mediator.Send(new AddFavoriteCommand(productId), ct);
            return NoContent();
        }

        /// <summary>
        /// openapi: DELETE /api/v1/favorites/{product_id} → 204 (идемпотентно).
        /// </summary>
        [HttpDelete("{product_id:guid}")]
        public async Task<IActionResult> RemoveFavorite(
            [FromRoute(Name = "product_id")] Guid productId, CancellationToken ct)
        {
            await _mediator.Send(new RemoveFavoriteCommand(productId), ct);
            return NoContent();
        }
        /// <summary>
        /// openapi: POST /api/v1/favorites/{product_id}/subscribe → 204.
        /// Тело опциональное; events default — [BACK_IN_STOCK, PRICE_DROP].
        /// </summary>
        [HttpPost("{product_id:guid}/subscribe")]
        public async Task<IActionResult> Subscribe(
            [FromRoute(Name = "product_id")] Guid productId,
            [FromBody] SubscribeRequest? request,
            CancellationToken ct)
        {
            var notifyOn = ParseEvents(request?.Events);
            await _mediator.Send(new SubscribeCommand(productId, notifyOn), ct);
            return NoContent();
        }

        /// <summary>
        /// openapi: DELETE /api/v1/favorites/{product_id}/subscribe → 204 (идемпотентно).
        /// </summary>
        [HttpDelete("{product_id:guid}/subscribe")]
        public async Task<IActionResult> Unsubscribe(
            [FromRoute(Name = "product_id")] Guid productId, CancellationToken ct)
        {
            await _mediator.Send(new UnsubscribeCommand(productId), ct);
            return NoContent();
        }

        /// <summary>
        /// openapi enum: [BACK_IN_STOCK, PRICE_DROP]. Если null/empty — дефолт «оба»,
        /// согласно openapi default. Неизвестное значение → 400 INVALID_REQUEST.
        /// </summary>
        private static NotifyOnDto ParseEvents(string[]? events)
        {
            // openapi default: [BACK_IN_STOCK, PRICE_DROP].
            if (events is null || events.Length == 0)
                return NotifyOnDto.InStock | NotifyOnDto.PriceDrop;

            var result = NotifyOnDto.None;
            foreach (var e in events)
            {
                result |= e switch
                {
                    "BACK_IN_STOCK" => NotifyOnDto.InStock,
                    "PRICE_DROP" => NotifyOnDto.PriceDrop,
                    _ => throw new DomainException(
                        $"Unknown event value: {e}. Allowed: BACK_IN_STOCK, PRICE_DROP",
                        "INVALID_REQUEST"),
                };
            }
            return result;
        }
    }
}