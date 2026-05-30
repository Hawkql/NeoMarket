using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleSkuOutOfStock
{
    public sealed class HandleSkuOutOfStockCommandValidator : AbstractValidator<HandleSkuOutOfStockCommand>
    {
        public HandleSkuOutOfStockCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey).NotEmpty();
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.SkuId).NotEmpty();
        }
    }
}
