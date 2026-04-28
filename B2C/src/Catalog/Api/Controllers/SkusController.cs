using Api.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/v1/products/{product_id:guid}/skus")]
    [Produces("application/json")]
    [Authorize] // Эндпоинты SKU требуют JWT согласно OpenAPI
    public sealed class SkusController(ISkuService skuService) : ControllerBase
    {
        /// <summary>Список SKU товара (кратко — для карточки товара)</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<SkuShortResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<SkuShortResponse>>> GetSkus(
            [FromRoute(Name = "product_id")] Guid productId,
            CancellationToken ct)
        {
            var skus = await skuService.GetSkusAsync(productId, ct);

            return Ok(skus.Select(s => new SkuShortResponse(
                s.Name,
                s.Price,
                new ImageResponse(s.Image.Url, s.Image.Order)
            )).ToList());
        }

        /// <summary>Конкретный SKU товара</summary>
        [HttpGet("{sku_id:guid}")]
        [ProducesResponseType(typeof(SkuResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SkuResponse>> GetSku(
            [FromRoute(Name = "product_id")] Guid productId,
            [FromRoute(Name = "sku_id")] Guid skuId,
            CancellationToken ct)
        {
            var sku = await skuService.GetSkuAsync(productId, skuId, ct);

            return Ok(new SkuResponse(
                sku.Id,
                sku.Name,
                sku.Price,
                sku.Quantity,
                sku.Characteristics.Select(c => new CharacteristicResponse(c.Name, c.Value)).ToList(),
                sku.Images.Select(i => new ImageResponse(i.Url, i.Order)).ToList()
            ));
        }
    }
}
