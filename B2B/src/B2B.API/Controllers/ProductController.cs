using B2B.Application.Common.Abstractions;
using B2B.Application.Products.Commands.CreateProduct;
using B2B.Application.Products.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{

    [ApiController]
    [Route("api/v1/products")]
    [Authorize(Policy = "SellerOnly")]    // только продавцы (US-B2B-01)
    public sealed class ProductsController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ICurrentUserService _currentUser;

        public ProductsController(ISender sender, ICurrentUserService currentUser)
        {
            _sender = sender;
            _currentUser = currentUser;
        }

        /// <summary>POST /api/v1/products — создание товара (US-B2B-01).</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductRequest request,
            CancellationToken ct)
        {
            // IDOR: seller_id берём ИЗ JWT, НЕ из тела запроса
            var command = new CreateProductCommand(
                SellerId: _currentUser.SellerId,
                CategoryId: request.CategoryId,
                Title: request.Title,
                Description: request.Description,
                Images: request.Images
                    .Select(i => new ImageInputDto(i.Url, i.Ordering))
                    .ToList(),
                Characteristics: (request.Characteristics ?? new())
                    .Select(c => new CharacteristicInputDto(c.Name, c.Value))
                    .ToList());

            var result = await _sender.Send(command, ct);

            // 201 Created с телом товара
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }

    // ============ HTTP Request DTOs (только то, что присылает клиент) ============
    // ВАЖНО: здесь НЕТ seller_id — клиент его не контролирует (IDOR).

    public sealed class CreateProductRequest
    {
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<ImageRequest> Images { get; set; } = new();
        public List<CharacteristicRequest>? Characteristics { get; set; }
    }

    public sealed class ImageRequest
    {
        public string Url { get; set; } = null!;
        public int Ordering { get; set; }
    }

    public sealed class CharacteristicRequest
    {
        public string Name { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

}
