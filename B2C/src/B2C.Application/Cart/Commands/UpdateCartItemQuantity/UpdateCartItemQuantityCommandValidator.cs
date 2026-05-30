using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Cart.Commands.UpdateCartItemQuantity
{
    public sealed class UpdateCartItemQuantityCommandValidator
        : AbstractValidator<UpdateCartItemQuantityCommand>
    {
        public UpdateCartItemQuantityCommandValidator()
        {
            RuleFor(x => x.SkuId).NotEmpty();
            RuleFor(x => x.Quantity).InclusiveBetween(1, 999);
        }
    }
}
