using Api.DTOs;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
namespace Api.Controllers
{
    [ApiController]
    [Route("api/v1/products")]
    [Produces("application/json")]
    public sealed class ProductsController(IProductService productService) : ControllerBase
    {
        /// <summary>Список товаров с фильтрами, поиском, пагинацией</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductListResponse>> List(
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0,
            [FromQuery] Guid? category_id = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sort = null,
            [FromQuery] Dictionary<string, string>? filters = null,
            CancellationToken ct = default)
        {
            if (search is not null && search.Trim().Length < 3)
                return BadRequest(new ErrorResponse("Search query must be at least 3 characters."));

            var result = await productService.ListAsync(
                category_id, search, filters, sort, limit, offset, ct);

            return Ok(MapList(result));
        }

        /// <summary>Полная карточка товара</summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponse>> GetById(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            var product = await productService.GetByIdAsync(id, ct);
            return Ok(MapProduct(product));
        }

        /// <summary>Похожие товары</summary>
        [HttpGet("{id:guid}/similar")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductListResponse>> GetSimilar(
            [FromRoute] Guid id,
            [FromQuery] Guid category,
            [FromQuery] int limit = 8,
            [FromQuery] int offset = 0,
            CancellationToken ct = default)
        {
            if (category == Guid.Empty)
                return BadRequest(new ErrorResponse("category query parameter is required."));

            var result = await productService.GetSimilarAsync(id, category, limit, offset, ct);
            return Ok(MapList(result));
        }

        // ─── Mapping ──────────────────────────────────────────────────────────────

        private static ProductListResponse MapList(ProductListDto dto) => new(
            dto.TotalCount,
            dto.Limit,
            dto.Offset,
            dto.Items.Select(p => new ProductShortResponse(
                p.Id, p.Title, p.Image, p.Price, p.InStock, p.IsInCart)).ToList());

        private static ProductResponse MapProduct(ProductDto dto) => new(
            dto.Id,
            dto.Slug,
            dto.Title,
            dto.Description,
            dto.Images.Select(i => new ImageResponse(i.Url, i.Order)).ToList(),
            dto.Status,
            dto.Characteristics.Select(c => new CharacteristicResponse(c.Name, c.Value)).ToList(),
            dto.Skus.Select(s => new SkuResponse(
                s.Id, s.Name, s.Price, s.Quantity,
                s.Characteristics.Select(c => new CharacteristicResponse(c.Name, c.Value)).ToList(),
                s.Images.Select(i => new ImageResponse(i.Url, i.Order)).ToList()
            )).ToList());
    }
}
