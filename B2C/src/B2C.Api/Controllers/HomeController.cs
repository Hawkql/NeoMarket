using B2C.Api.Contracts;
using B2C.Application.Common.Abstractions;
using B2C.Application.HomePage.Queries.GetActiveBanners;
using B2C.Application.HomePage.Queries.GetCollection;
using B2C.Application.HomePage.Queries.ListCollections;
using B2C.Application.HomePage.Queries.RecordBannerEvent;
using B2C.Domain.Common;
using B2C.Domain.HomePage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// Главная страница (US-CART-04/05): баннеры, подборки, CTR-аналитика.
    /// Публичный контроллер.
    /// </summary>
    [ApiController]
    [Route("api/v1/home")]
    [AllowAnonymous]
    public sealed class HomeController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly ISessionContext _session;

        public HomeController(
            IMediator mediator,
            ICurrentUserService currentUser,
            ISessionContext session)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _session = session;
        }

        /// <summary>US-CART-04: активные баннеры.</summary>
        [HttpGet("banners")]
        public async Task<IActionResult> GetBanners(CancellationToken ct)
        {
            var banners = await _mediator.Send(new GetActiveBannersQuery(), ct);
            return Ok(banners);
        }

        /// <summary>US-CART-05: список подборок (без товаров).</summary>
        [HttpGet("collections")]
        public async Task<IActionResult> ListCollections(CancellationToken ct)
        {
            var collections = await _mediator.Send(new ListCollectionsQuery(), ct);
            return Ok(collections);
        }

        /// <summary>US-CART-05: подборка с обогащёнными товарами (по slug).</summary>
        [HttpGet("collections/{slug}")]
        public async Task<IActionResult> GetCollection(string slug, CancellationToken ct)
        {
            var collection = await _mediator.Send(new GetCollectionQuery(slug), ct);
            return Ok(collection);
        }

        
    }
}
