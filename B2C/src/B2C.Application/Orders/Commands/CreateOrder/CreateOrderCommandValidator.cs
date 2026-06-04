using FluentValidation;

namespace B2C.Application.Orders.Commands.CreateOrder
{
    public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey).NotEmpty();
            RuleFor(x => x.AddressId).NotEmpty();
            RuleFor(x => x.PaymentMethodId).NotEmpty();
            RuleFor(x => x.Comment).MaximumLength(1000);
            RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item");
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.SkuId).NotEmpty();
                item.RuleFor(i => i.Quantity).InclusiveBetween(1, 999);
            });
        }
    }
}