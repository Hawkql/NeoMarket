using B2C.Application.Catalog.Dtos;
using B2C.Application.Catalog.Queries.GetBreadcrumbs;
using B2C.Application.Catalog.Queries.GetCategoryFilters;
using B2C.Application.Catalog.Queries.GetCategoryTree;
using B2C.Application.Catalog.Queries.GetFacets;
using B2C.Application.Catalog.Queries.GetProductDetail;
using B2C.Application.Catalog.Queries.GetSimilarProducts;
using B2C.Application.Catalog.Queries.ListCatalogProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{

    /// <summary>
    /// Публичный каталог (US-CAT-01..05). Доступен без авторизации.
    /// Проксирует запросы в B2B через query handlers (ACL).
    /// </summary>
    [ApiController]
    [Route("api/v1/catalog")]
    [AllowAnonymous]
    public sealed class CatalogController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CatalogController(IMediator mediator) => _mediator = mediator;


        /// <summary>US-CAT-05: дерево категорий.</summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategoryTree(CancellationToken ct)
        {
            var tree = await _mediator.Send(new GetCategoryTreeQuery(), ct);
            return Ok(tree);
        }

        /// <summary>US-CAT-01: доступные фильтры для категории.</summary>
        [HttpGet("categories/{categoryId:guid}/filters")]
        public async Task<IActionResult> GetCategoryFilters(Guid categoryId, CancellationToken ct)
        {
            var filters = await _mediator.Send(new GetCategoryFiltersQuery(categoryId), ct);
            return Ok(filters);
        }

        /// <summary>US-CAT-01: фасеты с подсчётом для текущей выборки.</summary>
        [HttpGet("facets")]
        public async Task<IActionResult> GetFacets(
            [FromQuery] Guid? categoryId,
            [FromQuery] string? search,
            [FromQuery] int? minPrice,
            [FromQuery] int? maxPrice,
            CancellationToken ct = default)
        {
            var appliedFilters = ExtractDynamicFilters();

            var facets = await _mediator.Send(new GetFacetsQuery(
                categoryId, search, appliedFilters, minPrice, maxPrice), ct);

            return Ok(facets);
        }

        /// <summary>US-CAT-05: хлебные крошки (по category_id ИЛИ product_id).</summary>
        [HttpGet("breadcrumbs")]
        public async Task<IActionResult> GetBreadcrumbs(
            [FromQuery] Guid? categoryId,
            [FromQuery] Guid? productId,
            CancellationToken ct = default)
        {
            var crumbs = await _mediator.Send(new GetBreadcrumbsQuery(categoryId, productId), ct);
            return Ok(crumbs);
        }

        // ===================== helpers =====================

        /// <summary>
        /// Извлекает динамические фильтры формата filter[slug]=value из query string.
        /// Например ?filter[brand]=apple&filter[color]=black → { brand: apple, color: black }.
        /// </summary>
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

       
    }
}
