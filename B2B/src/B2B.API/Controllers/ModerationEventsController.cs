using B2B.Api.Contracts;
using B2B.Application.Moderation.Commands.ApplyModerationDecision;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{

    [ApiController]
    [Route("api/v1/events")]
    [Authorize(Policy = "ServiceOnly", AuthenticationSchemes = "ServiceKey")]
    public sealed class ModerationEventsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ModerationEventsController(IMediator mediator) => _mediator = mediator;

        // POST /api/v1/events/moderation — вызывает Moderation-сервис
        [HttpPost("moderation")]
        public async Task<IActionResult> ApplyDecision(
            [FromBody] ModerationDecisionRequest request, CancellationToken ct)
        {
            var command = new ApplyModerationDecisionCommand(
                IdempotencyKey: request.IdempotencyKey,
                ProductId: request.ProductId,
                Status: request.Status,
                HardBlock: request.HardBlock,
                BlockingReason: request.BlockingReason,
                FieldReports: request.FieldReports);

            await _mediator.Send(command, ct);
            return Ok();   // 200, тело не требуется
        }
    }
}
