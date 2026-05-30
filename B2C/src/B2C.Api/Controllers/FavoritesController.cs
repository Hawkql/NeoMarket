using B2C.Application.Favorites.Commands.AddFavorite;
using B2C.Application.Favorites.Commands.RemoveFavorite;
using B2C.Application.Favorites.Queries.ListMyFavorites;
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

        /// <summary>Список избранного с обогащёнными данными о товарах из B2B.</summary>
        [HttpGet]
        public async Task<IActionResult> ListFavorites(CancellationToken ct)
        {
            var favorites = await _mediator.Send(new ListMyFavoritesQuery(), ct);
            return Ok(favorites);
        }

        /// <summary>Добавить товар в избранное (идемпотентно).</summary>
        [HttpPost("{productId:guid}")]
        public async Task<IActionResult> AddFavorite(Guid productId, CancellationToken ct)
        {
            await _mediator.Send(new AddFavoriteCommand(productId), ct);
            return NoContent();
        }

        /// <summary>Удалить из избранного (идемпотентно).</summary>
        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> RemoveFavorite(Guid productId, CancellationToken ct)
        {
            await _mediator.Send(new RemoveFavoriteCommand(productId), ct);
            return NoContent();
        }
    }
}
