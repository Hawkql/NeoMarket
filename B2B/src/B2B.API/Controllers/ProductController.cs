using B2B.Api.Contracts;
using B2B.Application.Common.Abstractions;
using B2B.Application.Products.Commands.CreateProduct;
using B2B.Application.Products.Commands.DeleteProduct;
using B2B.Application.Products.Commands.UpdateProduct;
using B2B.Application.Products.Dtos;
using B2B.Application.Products.Queries.GetProductById;
using B2B.Application.Products.Queries.ListProducts;
using B2B.Domain.Products;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{

    [ApiController]
    [Route("api/v1/products")]
    [Authorize(Policy = "SellerOnly")]
    public sealed class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public ProductsController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductRequest request, CancellationToken ct)
        {
            var command = new CreateProductCommand(
                SellerId: _currentUser.SellerId,   // из JWT, не из тела
                CategoryId: request.CategoryId,
                Title: request.Title,
                Description: request.Description,
                Characteristics: request.Characteristics,
                Images: request.Images);

            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] ProductStatus? status,
            [FromQuery(Name = "include_deleted")] bool includeDeleted = false,
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0,
            CancellationToken ct = default)
        {
            var query = new ListProductsQuery(
                SellerId: _currentUser.SellerId,
                Status: status,
                IncludeDeleted: includeDeleted,
                Limit: limit,
                Offset: offset);

            return Ok(await _mediator.Send(query, ct));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var query = new GetProductByIdQuery(id, _currentUser.SellerId);
            return Ok(await _mediator.Send(query, ct));
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
        {
            var command = new UpdateProductCommand(
                ProductId: id,
                SellerId: _currentUser.SellerId,
                Title: request.Title,
                Description: request.Description,
                CategoryId: request.CategoryId,
                Characteristics: request.Characteristics);

            return Ok(await _mediator.Send(command, ct));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteProductCommand(id, _currentUser.SellerId), ct);
            return NoContent();
        }
    }

}
