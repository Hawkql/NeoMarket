using B2B.Api.Contracts;
using B2B.Application.Common.Abstractions;
using B2B.Application.Skus.Commands.DeleteSku;
using B2B.Application.Skus.Commands.UpdateSku;
using B2B.Application.Skus.Commands.СreateSku;
using B2B.Application.Skus.Dtos;
using B2B.Application.Skus.Queries.GetSkuById;
using B2B.Application.Skus.Queries.ListSkusByProduct;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    [ApiController]
    [Route("api/v1")]
    [Authorize(Policy = "SellerOnly")]
    public sealed class SkusController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public SkusController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        // POST /api/v1/skus — product_id в теле
        [HttpPost("skus")]
        public async Task<IActionResult> Create(
            [FromBody] CreateSkuRequest request, CancellationToken ct)
        {
            var command = new CreateSkuCommand(
                SellerId: _currentUser.SellerId,
                ProductId: request.ProductId,
                Name: request.Name,
                Price: request.Price,
                Discount: request.Discount,
                CostPrice: request.CostPrice,
                Article: request.Article,
                Images: request.Images ?? new List<SkuImageInputDto>(),
                Characteristics: request.Characteristics ?? new List<SkuCharacteristicInputDto>());

            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // GET /api/v1/skus/{id}
        [HttpGet("skus/{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(
                new GetSkuByIdQuery(id, _currentUser.SellerId), ct);
            return Ok(result);
        }

        // GET /api/v1/products/{productId}/skus
        [HttpGet("products/{productId:guid}/skus")]
        public async Task<IActionResult> ListByProduct(Guid productId, CancellationToken ct)
        {
            var result = await _mediator.Send(
                new ListSkusByProductQuery(productId, _currentUser.SellerId), ct);
            return Ok(result);
        }

        // PATCH /api/v1/skus/{id}
        [HttpPatch("skus/{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateSkuRequest request, CancellationToken ct)
        {
            var command = new UpdateSkuCommand(
                SkuId: id,
                SellerId: _currentUser.SellerId,
                Name: request.Name,
                Price: request.Price,
                Discount: request.Discount,
                CostPrice: request.CostPrice,
                Article: request.Article,
                Characteristics: request.Characteristics);

            return Ok(await _mediator.Send(command, ct));
        }

        // DELETE /api/v1/skus/{id}
        [HttpDelete("skus/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteSkuCommand(id, _currentUser.SellerId), ct);
            return NoContent();
        }
    }
}
