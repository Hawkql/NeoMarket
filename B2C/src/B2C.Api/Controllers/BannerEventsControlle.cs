using B2C.Api.Contracts;
using B2C.Application.Common.Abstractions;
using B2C.Application.HomePage.Queries.RecordBannerEvent;
using B2C.Domain.Common;
using B2C.Domain.HomePage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// CTR-аналитика баннеров. US-CART-04 ожидает корневой route /api/v1/banner-events,
    /// отдельно от /home/banners (тот — листинг для главной).
    /// </summary>
    [ApiController]
    [Route("api/v1/banner-events")]
    [AllowAnonymous]  // гости тоже кликают по баннерам
    public sealed class BannerEventsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly ISessionContext _session;

        public BannerEventsController(
            IMediator mediator,
            ICurrentUserService currentUser,
            ISessionContext session)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _session = session;
        }

        [HttpPost]
        public async Task<IActionResult> Record(
            [FromBody] RecordBannerEventRequest request, CancellationToken ct)
        {
            // BuyerId если авторизован, иначе null. SessionId из X-Session-Id заголовка.
            var buyerId = _currentUser.IsAuthenticated
                ? _currentUser.BuyerId
                : (Guid?)null;

            await _mediator.Send(new RecordBannerEventCommand(
                request.BannerId,
                ParseEventType(request.Type),
                buyerId,
                _session.SessionId), ct);

            return NoContent();
        }

        private static BannerEventType ParseEventType(string type) =>
            type.ToLowerInvariant() switch
            {
                "impression" => BannerEventType.Impression,
                "click" => BannerEventType.Click,
                _ => throw new DomainException(
                    $"Unknown banner event type: {type}", "INVALID_REQUEST"),
            };
    }
}