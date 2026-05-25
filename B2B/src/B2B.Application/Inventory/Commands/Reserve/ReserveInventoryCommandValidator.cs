using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Inventory.Commands.Reserve
{
    public sealed class ReserveInventoryCommandValidator
    : AbstractValidator<ReserveInventoryCommand>
    {
        public ReserveInventoryCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey)
                .NotEqual(Guid.Empty).WithMessage("idempotency_key is required");

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
