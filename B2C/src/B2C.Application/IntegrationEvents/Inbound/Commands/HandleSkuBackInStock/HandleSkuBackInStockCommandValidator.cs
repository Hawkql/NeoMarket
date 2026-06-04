using FluentValidation;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleSkuBackInStock
{
    public sealed class HandleSkuBackInStockCommandValidator
        : AbstractValidator<HandleSkuBackInStockCommand>
    {
        public HandleSkuBackInStockCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey).NotEmpty();
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.SkuId).NotEmpty();
            RuleFor(x => x.AvailableQuantity).GreaterThanOrEqualTo(0);
        }
    }
}