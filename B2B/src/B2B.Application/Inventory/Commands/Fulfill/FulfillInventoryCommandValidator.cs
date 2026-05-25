using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Inventory.Commands.Fulfill
{
    public sealed class FulfillInventoryCommandValidator
    : AbstractValidator<FulfillInventoryCommand>
    {
        public FulfillInventoryCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEqual(Guid.Empty).WithMessage("order_id is required");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("items must contain at least one entry");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.SkuId).NotEqual(Guid.Empty);
                item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(1);
            });
        }
    }
}
