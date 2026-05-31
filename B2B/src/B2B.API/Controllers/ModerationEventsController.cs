using B2B.Api.Contracts;
using B2B.Application.Moderation.Commands.ApplyModerationDecision;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    [ApiController]
    [Route("api/v1/moderation")]                  // ← новый базовый путь по OpenAPI
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

        // POST /api/v1/moderation/events — receiveModerationEvent (по OpenAPI)
        [HttpPost("events")]
        public async Task<IActionResult> ReceiveModerationEvent(
            [FromBody] ModerationDecisionRequest request, CancellationToken ct)
        {
            var auth = CheckServiceKey();
            if (auth is not null) return auth;

            var command = new ApplyModerationDecisionCommand(
                IdempotencyKey: request.IdempotencyKey,
                ProductId: request.ProductId,
                EventType: request.EventType,
                HardBlock: request.HardBlock,
                BlockingReasonId: request.BlockingReasonId,
                ModeratorComment: request.ModeratorComment,
                ModeratorId: request.ModeratorId,
                FieldReports: request.FieldReports,
                OccurredAt: request.OccurredAt);

            await _mediator.Send(command, ct);
            return NoContent();                  // ← 204 No Content по OpenAPI
        }
    }
}