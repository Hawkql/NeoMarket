using System;
using System.Collections.Generic;
using MediatR;

namespace B2B.Application.Moderation.Commands.ApplyModerationDecision
{
    public sealed record ApplyModerationDecisionCommand(
        Guid IdempotencyKey,
        Guid ProductId,
        string EventType,                        // MODERATED | BLOCKED (по OpenAPI ModerationEventType)
        bool HardBlock,                          // при BLOCKED: true → HARD_BLOCKED
        Guid? BlockingReasonId,                  // обязательно при BLOCKED
        string? ModeratorComment,
        Guid? ModeratorId,
        IReadOnlyList<FieldReportInputDto>? FieldReports,
        DateTime OccurredAt
    ) : IRequest;
}