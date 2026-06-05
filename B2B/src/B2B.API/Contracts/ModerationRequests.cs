using System;
using System.Collections.Generic;
using B2B.Application.Moderation.Commands.ApplyModerationDecision;

namespace B2B.Api.Contracts
{
    /// <summary>
    /// Совпадает по форме с ModerationEventRequest из openapi.yaml (receiveModerationEvent),
    /// плюс необязательное расширение blocking_reason_title — Moderation присылает
    /// human-readable текст причины, B2B его хранит и отдаёт в карточке.
    /// </summary>
    public sealed record ModerationDecisionRequest(
        Guid IdempotencyKey,
        Guid ProductId,
        string EventType,
        bool HardBlock,
        Guid? BlockingReasonId,
        string? BlockingReasonTitle,
        string? ModeratorComment,
        Guid? ModeratorId,
        IReadOnlyList<FieldReportInputDto>? FieldReports,
        DateTime OccurredAt);
}