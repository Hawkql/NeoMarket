using FluentValidation;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductHardBlocked
{
    public sealed class HandleProductHardBlockedCommandValidator
        : AbstractValidator<HandleProductHardBlockedCommand>
    {
        public HandleProductHardBlockedCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey).NotEmpty();
            RuleFor(x => x.ProductId).NotEmpty();
        }
    }
}