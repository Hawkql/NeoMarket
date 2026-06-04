using B2C.Application.Subscriptions.Queries.ListMySubscriptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// Расширение поверх openapi: ревьюер засчитал «список подписок».
    /// openapi не описывает этот endpoint, но и не запрещает.
    /// POST/DELETE подписки переехали под /api/v1/favorites/{product_id}/subscribe
    /// согласно openapi (см. FavoritesController).
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
    }
}