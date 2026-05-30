using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductBlocked
{
    public sealed class HandleProductBlockedCommandValidator : AbstractValidator<HandleProductBlockedCommand>
    {
        public HandleProductBlockedCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey).NotEmpty();
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.SkuIds).NotNull();
        }
    }
}
