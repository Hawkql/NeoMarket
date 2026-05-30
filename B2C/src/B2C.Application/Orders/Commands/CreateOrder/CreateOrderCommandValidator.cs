using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Orders.Commands.CreateOrder
{
    public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey).NotEmpty();
            RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(500);

            RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item");
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.SkuId).NotEmpty();
                item.RuleFor(i => i.Quantity).InclusiveBetween(1, 999);
            });
        }
    }
}
