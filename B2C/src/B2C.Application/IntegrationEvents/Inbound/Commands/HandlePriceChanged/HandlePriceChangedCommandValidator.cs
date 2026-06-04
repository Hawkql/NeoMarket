using FluentValidation;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandlePriceChanged
{
    public sealed class HandlePriceChangedCommandValidator
        : AbstractValidator<HandlePriceChangedCommand>
    {
        public HandlePriceChangedCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey).NotEmpty();
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.SkuId).NotEmpty();
            RuleFor(x => x.OldPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.NewPrice).GreaterThanOrEqualTo(0);
        }
    }
}