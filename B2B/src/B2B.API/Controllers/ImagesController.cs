using B2B.Api.Contracts;
using B2B.Application.Common.Abstractions;
using B2B.Application.Images.Commands.UploadImage;
using B2B.Domain.Common;
using B2B.Domain.Images;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    [ApiController]
    [Route("api/v1/images")]
    [Authorize(Policy = "SellerOnly")]
    public sealed class ImagesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public ImagesController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        // POST /api/v1/images — multipart/form-data
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
    [FromForm] ImageUploadRequest request,
    CancellationToken ct = default)
        {
            var file = request.File;
            if (file is null || file.Length == 0)
                throw new DomainException("file is required", "INVALID_REQUEST");

            var type = request.EntityType?.Trim().ToLowerInvariant() switch
            {
                "product" => ImageEntityType.Product,
                "sku" => ImageEntityType.Sku,
                _ => throw new DomainException(
                    "entity_type must be 'product' or 'sku'", "INVALID_REQUEST")
            };

            await using var stream = file.OpenReadStream();

            var command = new UploadImageCommand(
                SellerId: _currentUser.SellerId,
                FileStream: stream,
                DeclaredContentType: file.ContentType,
                EntityType: type,
                EntityId: request.EntityId,
                Ordering: request.Ordering);

            var result = await _mediator.Send(command, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
