using B2C.Application.Catalog.Dtos;
using B2C.Application.Catalog.Queries.GetProductDetail;
using B2C.Application.Catalog.Queries.GetSimilarProducts;
using B2C.Application.Catalog.Queries.ListCatalogProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// Каталог товаров для покупателя (US-CAT-01/02/03/04).
    /// Корневой /products — отдельно от иерархических /catalog/categories.
    /// </summary>
    [ApiController]
    [Route("api/v1/products")]
    [AllowAnonymous]
    public sealed class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProductsController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// US-CAT-01/02: список товаров с фильтрами, поиском, сортировкой, пагинацией.
        /// Фильтры — динамические query-параметры filter[slug]=value, собираем из Request.Query.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? categoryId,
            [FromQuery] string? search,
            [FromQuery] int? minPrice,
            [FromQuery] int? maxPrice,
            [FromQuery] string sort = "rating",
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0,
            CancellationToken ct = default)
        {
            var filters = ExtractDynamicFilters();
            var result = await _mediator.Send(new ListCatalogProductsQuery(
                categoryId,
                search,
                filters,
                minPrice,
                maxPrice,
                ParseSort(sort),
                limit,
                offset), ct);
            return Ok(result);
        }

        /// <summary>US-CAT-03: карточка товара (без cost_price / reserved_quantity).</summary>
        [HttpGet("{productId:guid}")]
        public async Task<IActionResult> Card(Guid productId, CancellationToken ct)
        {
            var card = await _mediator.Send(new GetProductDetailQuery(productId), ct);
            return Ok(card);
        }

        /// <summary>US-CAT-04: похожие товары (default limit 10, override через ?limit=).</summary>
        [HttpGet("{productId:guid}/similar")]
        public async Task<IActionResult> Similar(
            Guid productId,
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            var similar = await _mediator.Send(
                new GetSimilarProductsQuery(productId, limit), ct);
            return Ok(similar);
        }

        // --- Локальные хелперы (перенесены 1-в-1 из старого CatalogController.ListProducts) ---

        private IReadOnlyDictionary<string, string>? ExtractDynamicFilters()
        {
            var result = new Dictionary<string, string>();
            foreach (var (key, value) in Request.Query)
            {
                // Ключи вида filter[brand]
                if (key.StartsWith("filter[", StringComparison.Ordinal) && key.EndsWith(']'))
                {
                    var slug = key[7..^1];  // вырезаем filter[ ... ]
                    if (!string.IsNullOrWhiteSpace(slug))
                        result[slug] = value.ToString();
                }
            }
            return result.Count > 0 ? result : null;
        }

        private static CatalogSortDto ParseSort(string sort) => sort.ToLowerInvariant() switch
        {
            "rating" => CatalogSortDto.Rating,
            "popularity" => CatalogSortDto.Popularity,
            "price_asc" => CatalogSortDto.PriceAsc,
            "price_desc" => CatalogSortDto.PriceDesc,
            "date_desc" => CatalogSortDto.DateDesc,
            "discount_desc" => CatalogSortDto.DiscountDesc,
            _ => CatalogSortDto.Rating,
        };
    }
}