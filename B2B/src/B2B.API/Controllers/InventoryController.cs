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
    [AllowAnonymous]   // авторизация — ручная проверка X-Service-Key (как в каталоге)
    public sealed class InventoryController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IConfiguration _config;

        public InventoryController(IMediator mediator, IConfiguration config)
        {
            _mediator = mediator;
            _config = config;
        }

        private IActionResult? CheckServiceKey()
        {
            var expected = _config["ServiceKey:Incoming"];
            var provided = Request.Headers["X-Service-Key"].ToString();
            if (string.IsNullOrEmpty(expected) || provided != expected)
                return StatusCode(StatusCodes.Status401Unauthorized,
                    new { code = "UNAUTHORIZED", message = "Invalid or missing service key" });
            return null;
        }

        [HttpPost("reserve")]
        [HttpPost("/api/v1/reserve")]
        public async Task<IActionResult> Reserve(
            [FromBody] ReserveRequest request, CancellationToken ct)
        {
            var auth = CheckServiceKey();
            if (auth is not null) return auth;

            var command = new ReserveInventoryCommand(
                IdempotencyKey: request.IdempotencyKey,
                OrderId: request.OrderId,
                Items: request.Items);
            return Ok(await _mediator.Send(command, ct));
        }

        [HttpPost("unreserve")]
        [HttpPost("/api/v1/unreserve")]
        public async Task<IActionResult> Unreserve(
            [FromBody] InventoryOrderRequest request, CancellationToken ct)
        {
            var auth = CheckServiceKey();
            if (auth is not null) return auth;

            var command = new UnreserveInventoryCommand(request.OrderId, request.Items);
            return Ok(await _mediator.Send(command, ct));
        }

        [HttpPost("fulfill")]
        [HttpPost("/api/v1/fulfill")]
        public async Task<IActionResult> Fulfill(
            [FromBody] InventoryOrderRequest request, CancellationToken ct)
        {
            var auth = CheckServiceKey();
            if (auth is not null) return auth;

            var command = new FulfillInventoryCommand(request.OrderId, request.Items);
            return Ok(await _mediator.Send(command, ct));
        }
    }
}
