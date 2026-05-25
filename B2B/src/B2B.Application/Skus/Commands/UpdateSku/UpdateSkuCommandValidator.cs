using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Skus.Commands.UpdateSku
{
    public sealed class UpdateSkuCommandValidator
    : AbstractValidator<UpdateSkuCommand>
    {
        public UpdateSkuCommandValidator()
        {
            When(x => x.Name is not null, () =>
            {
                RuleFor(x => x.Name!)
                    .NotEmpty().WithMessage("name must not be empty")
                    .MaximumLength(255).WithMessage("name must be 1-255 characters");
            });

            When(x => x.Price is not null, () =>
            {
                RuleFor(x => x.Price!.Value)
                    .GreaterThanOrEqualTo(0).WithMessage("price must be >= 0 (kopecks)");
            });

            When(x => x.Discount is not null, () =>
            {
                RuleFor(x => x.Discount!.Value)
                    .GreaterThanOrEqualTo(0).WithMessage("discount must be >= 0");
            });

            When(x => x.CostPrice is not null, () =>
            {
                RuleFor(x => x.CostPrice!.Value)
                    .GreaterThanOrEqualTo(0).WithMessage("cost_price must be >= 0");
            });

            When(x => x.Characteristics is not null, () =>
            {
                RuleForEach(x => x.Characteristics!).ChildRules(ch =>
                {
                    ch.RuleFor(c => c.Name).NotEmpty().WithMessage("characteristic name is required");
                    ch.RuleFor(c => c.Value).NotEmpty().WithMessage("characteristic value is required");
                });
            });
        }
    }
}
