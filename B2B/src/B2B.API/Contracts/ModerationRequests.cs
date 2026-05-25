using B2B.Application.Moderation.Commands.ApplyModerationDecision;

namespace B2B.Api.Contracts
{
    public sealed record ModerationDecisionRequest(
        Guid IdempotencyKey,
        Guid ProductId,
        string Status,
        bool HardBlock,
        BlockingReasonInputDto? BlockingReason,
        IReadOnlyList<FieldReportInputDto>? FieldReports);
}
