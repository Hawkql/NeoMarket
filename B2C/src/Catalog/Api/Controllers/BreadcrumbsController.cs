using Api.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public sealed class BreadcrumbsController(IBreadcrumbService breadcrumbService) : ControllerBase
    {
        /// <summary>
        /// Построение навигационной цепочки от корня до текущей категории или товара.
        /// Необходимо передать ровно один параметр: category_id или product_id.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(BreadcrumbResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<BreadcrumbResponse>> GetBreadcrumbs(
            [FromQuery] Guid? category_id = null,
            [FromQuery] Guid? product_id = null,
            [FromQuery] string lang = "ru",
            CancellationToken ct = default)
        {
            // Ранняя валидация — сервис тоже проверяет, но это даёт чистый HTTP 400
            if (category_id is null && product_id is null)
                return BadRequest(new ErrorResponse("category_id or product_id must be provided."));

            if (category_id is not null && product_id is not null)
                return BadRequest(new ErrorResponse("Only one of category_id or product_id must be provided."));

            var result = await breadcrumbService.BuildAsync(category_id, product_id, lang, ct);

            Response.Headers.CacheControl = "public, max-age=300";

            return Ok(new BreadcrumbResponse(
                result.Data.Select(b => new BreadcrumbItemResponse(
                    b.Id, b.Slug, b.Name, b.Url, b.Level, b.IsCurrent)).ToList(),
                new BreadcrumbMetaResponse(
                    result.Meta.ResolvedVia, result.Meta.CategoryId, result.Meta.ProductId)));
        }
    }
}
