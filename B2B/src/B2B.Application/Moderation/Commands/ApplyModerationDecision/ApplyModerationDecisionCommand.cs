using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Application.Moderation.Commands.ApplyModerationDecision
{
    public sealed record ApplyModerationDecisionCommand(
    Guid IdempotencyKey,
    Guid ProductId,
    string Status,                          // MODERATED | BLOCKED
    bool HardBlock,                         // значим при BLOCKED
    BlockingReasonInputDto? BlockingReason, // обязателен при BLOCKED
    IReadOnlyList<FieldReportInputDto>? FieldReports
) : IRequest;   
}
