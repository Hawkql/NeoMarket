using System.Text.Json;
using Api.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public sealed class CategoryFiltersController(ICategoryFilterService filterService) : ControllerBase
    {
        /// <summary>Список доступных фильтров для категории (sidebar фильтрации)</summary>
        [HttpGet("api/v1/categories/{id:guid}/filters")]
        [ProducesResponseType(typeof(FiltersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FiltersResponse>> GetFilters(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            var result = await filterService.GetFiltersAsync(id, ct);

            return Ok(new FiltersResponse(
                result.Items.Select(f => new FilterResponse(
                    f.Slug, f.Name, f.Type, f.Value, f.Min, f.Max)).ToList()));
        }

        /// <summary>
        /// Фасеты с подсчётом товаров по каждому значению фильтра.
        /// Вызывается при загрузке страницы категории и при каждом изменении фильтров.
        /// </summary>
        [HttpGet("api/v1/catalog/facets")]
        [ProducesResponseType(typeof(FacetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FacetsResponse>> GetFacets(
            [FromQuery] Guid category_id,
            [FromQuery] string? filters = null,
            CancellationToken ct = default)
        {
            if (category_id == Guid.Empty)
                return BadRequest(new ErrorResponse("category_id is required."));

            Dictionary<string, string>? activeFilters = null;
            if (!string.IsNullOrWhiteSpace(filters))
            {
                try
                {
                    activeFilters = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        Uri.UnescapeDataString(filters));
                }
                catch
                {
                    // malformed JSON — игнорируем фильтры, не падаем
                }
            }

            var result = await filterService.GetFacetsAsync(category_id, activeFilters, ct);

            return Ok(new FacetsResponse(
                result.CategoryId,
                result.Facets.Select(f => new FacetResponse(
                    f.Name,
                    f.Values.Select(v => new FacetValueResponse(v.Value, v.Count)).ToList()
                )).ToList()));
        }
    }
}
