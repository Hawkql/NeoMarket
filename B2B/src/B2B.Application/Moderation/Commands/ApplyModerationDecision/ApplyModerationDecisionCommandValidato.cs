using System;
using System.Linq;
using FluentValidation;

namespace B2B.Application.Moderation.Commands.ApplyModerationDecision
{
    public sealed class ApplyModerationDecisionCommandValidator
        : AbstractValidator<ApplyModerationDecisionCommand>
    {
        private static readonly string[] AllowedEventTypes = { "MODERATED", "BLOCKED" };

        public ApplyModerationDecisionCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey).NotEqual(Guid.Empty);
            RuleFor(x => x.ProductId).NotEqual(Guid.Empty);

            RuleFor(x => x.EventType)
                .NotEmpty()
                .Must(s => AllowedEventTypes.Contains(s))
                .WithMessage("event_type must be MODERATED or BLOCKED");

            RuleFor(x => x.OccurredAt).NotEqual(default(DateTime));

            // При BLOCKED обязателен blocking_reason_id
            When(x => x.EventType == "BLOCKED", () =>
            {
                RuleFor(x => x.BlockingReasonId)
                    .NotNull()
                    .NotEqual(Guid.Empty)
                    .WithMessage("blocking_reason_id is required when event_type is BLOCKED");

                When(x => x.FieldReports is not null, () =>
                {
                    RuleForEach(x => x.FieldReports!).ChildRules(fr =>
                    {
                        fr.RuleFor(f => f.FieldName).NotEmpty();
                        fr.RuleFor(f => f.Comment).NotEmpty();
                    });
                });
            });
        }
    }
}