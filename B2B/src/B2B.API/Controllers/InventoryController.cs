using B2B.Api.Contracts;
using B2B.Application.Inventory.Commands.Fulfill;
using B2B.Application.Inventory.Commands.Reserve;
using B2B.Application.Inventory.Commands.Unreserve;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    [ApiController]
    [Route("api/v1/inventory")]
    [Authorize(Policy = "ServiceOnly", AuthenticationSchemes = "ServiceKey")]
    public sealed class InventoryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InventoryController(IMediator mediator) => _mediator = mediator;

        [HttpPost("reserve")]                    // /api/v1/inventory/reserve (спека)
        [HttpPost("/api/v1/reserve")]            // /api/v1/reserve (flows) — алиас
        public async Task<IActionResult> Reserve(
            [FromBody] ReserveRequest request, CancellationToken ct)
        {
            var command = new ReserveInventoryCommand(
                IdempotencyKey: request.IdempotencyKey,
                OrderId: request.OrderId,
                Items: request.Items);

            return Ok(await _mediator.Send(command, ct));
        }
        [HttpPost("unreserve")]                  // /api/v1/inventory/unreserve (спека)
        [HttpPost("/api/v1/unreserve")]          // /api/v1/unreserve (flows) — алиас
        public async Task<IActionResult> Unreserve(
            [FromBody] InventoryOrderRequest request, CancellationToken ct)
        {
            var command = new UnreserveInventoryCommand(request.OrderId, request.Items);
            return Ok(await _mediator.Send(command, ct));
        }

        [HttpPost("fulfill")]                     // /api/v1/inventory/fulfill (спека)
        [HttpPost("/api/v1/fulfill")]             // /api/v1/fulfill (flows) — алиас
        public async Task<IActionResult> Fulfill(
            [FromBody] InventoryOrderRequest request, CancellationToken ct)
        {
            var command = new FulfillInventoryCommand(request.OrderId, request.Items);
            return Ok(await _mediator.Send(command, ct));
        }
    }
}
