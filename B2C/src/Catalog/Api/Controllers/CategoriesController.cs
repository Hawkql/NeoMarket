using Api.DTOs;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/v1/categories")]
    [Produces("application/json")]
    public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
    {
        /// <summary>Дерево категорий для навигации и левого меню витрины</summary>
        [HttpGet]
        [ProducesResponseType(typeof(CategoryTreeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<ActionResult<CategoryTreeResponse>> GetTree(CancellationToken ct)
        {
            var tree = await categoryService.GetTreeAsync(ct);
            return Ok(MapTree(tree));
        }

        /// <summary>Детальная информация о категории с SEO-данными и мета-тегами</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(CategoryDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<CategoryDetailResponse>> GetDetail(
            [FromRoute] Guid id,
            [FromQuery] bool include_product_count = false,
            [FromQuery] string lang = "ru",
            CancellationToken ct = default)
        {
            var detail = await categoryService.GetDetailAsync(id, include_product_count, lang, ct);

            Response.Headers.CacheControl = "public, max-age=3600";
            return Ok(MapDetail(detail));
        }

        // ─── Mapping ──────────────────────────────────────────────────────────────

        private static CategoryTreeResponse MapTree(CategoryTreeDto dto) =>
            new(dto.Items.Select(MapNode).ToList());

        private static CategoryNodeResponse MapNode(CategoryNodeDto dto) =>
            new(dto.Id, dto.Name, dto.ParentId, dto.Children.Select(MapNode).ToList());

        private static CategoryDetailResponse MapDetail(CategoryDetailDto dto) => new(
            dto.Id,
            dto.Name,
            dto.Slug,
            dto.Description,
            dto.Parent is null ? null
                : new CategoryParentResponse(dto.Parent.Id, dto.Parent.Name, dto.Parent.Slug),
            dto.ProductCount,
            new CategorySeoResponse(dto.Seo.Title, dto.Seo.Description, dto.Seo.Keywords),
            new CategoryMetaResponse(
                dto.MetaTags.OgTitle, dto.MetaTags.OgDescription,
                dto.MetaTags.OgImage, dto.MetaTags.TwitterCard),
            dto.ImageUrl,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}
