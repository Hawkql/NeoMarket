using B2B.Api.Contracts;
using B2B.Application.PublicCatalog.Queries.BatchPublicProducts;
using B2B.Application.PublicCatalog.Queries.GetPublicProduct;
using B2B.Application.PublicCatalog.Queries.GetPublicSimilar;
using B2B.Application.PublicCatalog.Queries.GetPublicSku;
using B2B.Application.PublicCatalog.Queries.ListPublicProducts;
using B2B.Domain.Products;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    [ApiController]
    [Route("api/v1/public")]
    [Authorize(Policy = "ServiceOnly", AuthenticationSchemes = "ServiceKey")]
    public sealed class PublicCatalogController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PublicCatalogController(IMediator mediator) => _mediator = mediator;

        // GET /api/v1/public/products
        [HttpGet("products")]
        public async Task<IActionResult> List(
            [FromQuery(Name = "category_id")] Guid? categoryId,
            [FromQuery] string? search,
            [FromQuery(Name = "min_price")] int? minPrice,
            [FromQuery(Name = "max_price")] int? maxPrice,
            [FromQuery(Name = "seller_id")] Guid? sellerId,
            [FromQuery] string? sort,
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0,
            CancellationToken ct = default)
        {
            var query = new ListPublicProductsQuery(
                CategoryId: categoryId,
                Search: search,
                MinPrice: minPrice,
                MaxPrice: maxPrice,
                SellerId: sellerId,
                Sort: ParseSort(sort),
                Limit: limit,
                Offset: offset);

            return Ok(await _mediator.Send(query, ct));
        }

        // POST /api/v1/public/products/batch
        [HttpPost("products/batch")]
        public async Task<IActionResult> Batch(
            [FromBody] BatchProductsRequest request, CancellationToken ct)
        {
            return Ok(await _mediator.Send(
                new BatchPublicProductsQuery(request.ProductIds), ct));
        }

        // GET /api/v1/public/products/{id}
        [HttpGet("products/{id:guid}")]
        public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new GetPublicProductQuery(id), ct));
        }

        // GET /api/v1/public/products/{id}/similar
        [HttpGet("products/{id:guid}/similar")]
        public async Task<IActionResult> Similar(
            Guid id, [FromQuery] int limit = 10, CancellationToken ct = default)
        {
            return Ok(await _mediator.Send(new GetPublicSimilarQuery(id, limit), ct));
        }

        // GET /api/v1/public/skus/{id}
        [HttpGet("skus/{id:guid}")]
        public async Task<IActionResult> GetSku(Guid id, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new GetPublicSkuQuery(id), ct));
        }

        // sort строка → enum. popular → CreatedDesc (нет данных о популярности, см. ADR)
        private static PublicSort ParseSort(string? sort) =>
            sort?.Trim().ToLowerInvariant() switch
            {
                "price_asc" => PublicSort.PriceAsc,
                "price_desc" => PublicSort.PriceDesc,
                "created_desc" => PublicSort.CreatedDesc,
                "popular" => PublicSort.CreatedDesc,   // нет метрики популярности
                _ => PublicSort.CreatedDesc            // дефолт
            };
    }
}
