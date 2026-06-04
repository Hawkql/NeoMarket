using B2C.Application.Catalog.Dtos;
using B2C.Application.Catalog.Queries.GetBreadcrumbs;
using B2C.Application.Catalog.Queries.GetCategoryFilters;
using B2C.Application.Catalog.Queries.GetCategoryTree;
using B2C.Application.Catalog.Queries.GetFacets;
using B2C.Application.Catalog.Queries.GetProductDetail;
using B2C.Application.Catalog.Queries.GetSimilarProducts;
using B2C.Application.Catalog.Queries.ListCatalogProducts;
using B2C.Application.Catalog.Queries.ListCategories;
using B2C.Application.HomePage.Queries.GetActiveBanners;
using B2C.Application.HomePage.Queries.GetCollection;
using B2C.Application.HomePage.Queries.ListCollections;
using B2C.Domain.Common;
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


        /// <summary>
        /// US-CAT-05: openapi GET /api/v1/catalog/categories — плоский список CategoryRef[].
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> ListCategories(CancellationToken ct)
        {
            var list = await _mediator.Send(new ListCategoriesQuery(), ct);
            return Ok(list);
        }

        /// <summary>
        /// US-CAT-05: openapi GET /api/v1/catalog/categories/tree — дерево CategoryTreeNode[].
        /// EnsureNoOrphans внутри handler'а бросает 422 при битой иерархии.
        /// </summary>
        [HttpGet("categories/tree")]
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
        /// <summary>
        /// US-CAT-01/02: openapi GET /api/v1/catalog/products.
        /// Поиск через параметр `q` (openapi), фильтры filter[slug]=value, sort enum.
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> ListProducts(
            [FromQuery] Guid? categoryId,
            [FromQuery] string? q,
            [FromQuery] int? minPrice,
            [FromQuery] int? maxPrice,
            [FromQuery] string sort = "popularity",
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0,
            CancellationToken ct = default)
        {
            var filters = ExtractDynamicFilters();
            var result = await _mediator.Send(new ListCatalogProductsQuery(
                categoryId,
                q,
                filters,
                minPrice,
                maxPrice,
                ParseSort(sort),
                limit,
                offset), ct);
            return Ok(result);
        }
        /// <summary>
        /// US-CART-04: openapi GET /api/v1/catalog/banners — активные баннеры.
        /// </summary>
        [HttpGet("banners")]
        public async Task<IActionResult> GetBanners(CancellationToken ct)
        {
            var banners = await _mediator.Send(new GetActiveBannersQuery(), ct);
            return Ok(banners);
        }

        /// <summary>
        /// US-CART-05: openapi GET /api/v1/catalog/collections — подборки + обогащённые товары.
        /// </summary>
        [HttpGet("collections")]
        public async Task<IActionResult> GetCollections(CancellationToken ct)
        {
            var collections = await _mediator.Send(new ListCollectionsQuery(), ct);
            return Ok(collections);
        }
        /// <summary>
        /// Расширение поверх openapi: карточка коллекции по id с обогащёнными товарами.
        /// </summary>
        [HttpGet("collections/{id:guid}")]
        public async Task<IActionResult> GetCollection(Guid id, CancellationToken ct)
        {
            var collection = await _mediator.Send(new GetCollectionQuery(id), ct);
            return Ok(collection);
        }
        /// <summary>US-CAT-03: openapi GET /api/v1/catalog/products/{product_id}.</summary>
        [HttpGet("products/{product_id:guid}")]
        public async Task<IActionResult> GetProduct(
            [FromRoute(Name = "product_id")] Guid productId, CancellationToken ct)
        {
            var product = await _mediator.Send(new GetProductDetailQuery(productId), ct);
            return Ok(product);
        }

        /// <summary>US-CAT-04: openapi GET /api/v1/catalog/products/{product_id}/similar.</summary>
        [HttpGet("products/{product_id:guid}/similar")]
        public async Task<IActionResult> GetSimilar(
            [FromRoute(Name = "product_id")] Guid productId,
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            if (limit < 1 || limit > 50)
                throw new DomainException(
                    "limit must be between 1 and 50", "INVALID_REQUEST");

            var similar = await _mediator.Send(new GetSimilarProductsQuery(productId, limit), ct);
            return Ok(similar);
        }

        private static readonly string[] AllowedSortValues =
            { "price_asc", "price_desc", "popularity", "new" };

        /// <summary>
        /// openapi DoD: invalid_sort_returns_400 — не молчком fallback'ить,
        /// а явно возвращать 400 со списком допустимых значений.
        /// </summary>
        private static CatalogSortDto ParseSort(string sort) =>
            sort.ToLowerInvariant() switch
            {
                "popularity" => CatalogSortDto.Popularity,
                "price_asc" => CatalogSortDto.PriceAsc,
                "price_desc" => CatalogSortDto.PriceDesc,
                "new" => CatalogSortDto.New,
                _ => throw new DomainException(
                    $"sort must be one of: {string.Join(", ", AllowedSortValues)}",
                    "INVALID_REQUEST"),
            };
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
