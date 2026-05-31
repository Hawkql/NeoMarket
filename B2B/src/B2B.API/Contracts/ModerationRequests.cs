using System;
using System.Collections.Generic;
using B2B.Application.Moderation.Commands.ApplyModerationDecision;

namespace B2B.Api.Contracts
{
    /// <summary>
    /// Совпадает по форме с ModerationEventRequest из openapi.yaml (receiveModerationEvent).
    /// </summary>
    public sealed record ModerationDecisionRequest(
        Guid IdempotencyKey,
        Guid ProductId,
        string EventType,
        bool HardBlock,
        Guid? BlockingReasonId,
        string? ModeratorComment,
        Guid? ModeratorId,
        IReadOnlyList<FieldReportInputDto>? FieldReports,
        DateTime OccurredAt);
}