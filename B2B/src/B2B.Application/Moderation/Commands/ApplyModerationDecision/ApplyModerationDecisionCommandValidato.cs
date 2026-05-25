using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Moderation.Commands.ApplyModerationDecision
{
    public sealed class ApplyModerationDecisionCommandValidator
    : AbstractValidator<ApplyModerationDecisionCommand>
    {
        private static readonly string[] AllowedStatuses = { "MODERATED", "BLOCKED" };

        public ApplyModerationDecisionCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey).NotEqual(Guid.Empty);
            RuleFor(x => x.ProductId).NotEqual(Guid.Empty);

            RuleFor(x => x.Status)
                .NotEmpty()
                .Must(s => AllowedStatuses.Contains(s))
                .WithMessage("status must be MODERATED or BLOCKED");

            // При BLOCKED обязательна причина
            When(x => x.Status == "BLOCKED", () =>
            {
                RuleFor(x => x.BlockingReason)
                    .NotNull().WithMessage("blocking_reason is required when status is BLOCKED");

                When(x => x.BlockingReason is not null, () =>
                {
                    RuleFor(x => x.BlockingReason!.Title)
                        .NotEmpty().WithMessage("blocking_reason.title is required");
                });

                // Если есть field_reports — каждый с непустым comment
                When(x => x.FieldReports is not null, () =>
                {
                    RuleForEach(x => x.FieldReports!).ChildRules(fr =>
                    {
                        fr.RuleFor(f => f.FieldName).NotEmpty();
                        fr.RuleFor(f => f.Comment).NotEmpty()
                            .WithMessage("field report comment is required");
                    });
                });
            });
        }
    }
}
