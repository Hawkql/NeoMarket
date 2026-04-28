using B2B.Application.Products.Commands.CreateProduct;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    [ApiController]
    [Route("api/v1/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProductsController(IMediator mediator) { _mediator = mediator; }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductRequest req, CancellationToken ct)
        {
            var command = new CreateProductCommand(
                req.Title, req.Description, req.CategoryId, req.SellerId, req.Characteristics);
            var id = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var product = await _mediator.Send(new GetProductByIdQuery(id), ct);
            return product == null ? NotFound() : Ok(product);
        }
    }

    public record CreateProductRequest(
        string Title,
        string Description,
        Guid CategoryId,
        Guid SellerId,
        List<CharacteristicDto> Characteristics);
}
