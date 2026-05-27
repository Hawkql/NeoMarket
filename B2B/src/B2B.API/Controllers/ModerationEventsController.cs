using B2B.Api.Contracts;
using B2B.Application.Moderation.Commands.ApplyModerationDecision;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{

    [ApiController]
    [Route("api/v1/events")]
    [AllowAnonymous]
    public sealed class ModerationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IConfiguration _config;

        public ModerationController(IMediator mediator, IConfiguration config)
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

        [HttpPost("moderation")]
        public async Task<IActionResult> ApplyDecision(
            [FromBody] ModerationDecisionRequest request, CancellationToken ct)
        {
            var auth = CheckServiceKey();
            if (auth is not null) return auth;

            var command = new ApplyModerationDecisionCommand(
                IdempotencyKey: request.IdempotencyKey,
                ProductId: request.ProductId,
                Status: request.Status,
                HardBlock: request.HardBlock,
                BlockingReason: request.BlockingReason,
                FieldReports: request.FieldReports);
            await _mediator.Send(command, ct);
            return Ok(new { ok = true });
        }
    }
}
