using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Skus.Commands.СreateSku
{
    public sealed class CreateSkuCommandValidator
    : AbstractValidator<CreateSkuCommand>
    {
        public CreateSkuCommandValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEqual(Guid.Empty).WithMessage("product_id is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("name is required")
                .MaximumLength(255).WithMessage("name must be 1-255 characters");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("price must be >= 0 (kopecks)");

            RuleFor(x => x.Discount)
                .GreaterThanOrEqualTo(0).WithMessage("discount must be >= 0");

            When(x => x.CostPrice is not null, () =>
            {
                RuleFor(x => x.CostPrice!.Value)
                    .GreaterThanOrEqualTo(0).WithMessage("cost_price must be >= 0");
            });

            RuleForEach(x => x.Images).ChildRules(img =>
            {
                img.RuleFor(i => i.Url).NotEmpty().WithMessage("image url is required");
                img.RuleFor(i => i.Ordering).GreaterThanOrEqualTo(0);
            });

            RuleForEach(x => x.Characteristics).ChildRules(ch =>
            {
                ch.RuleFor(c => c.Name).NotEmpty().WithMessage("characteristic name is required");
                ch.RuleFor(c => c.Value).NotEmpty().WithMessage("characteristic value is required");
            });
        }
    }
}
