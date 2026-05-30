using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Cart.Commands.AddToCart
{
    public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
    {
        public AddToCartCommandValidator()
        {
            RuleFor(x => x.SkuId).NotEmpty();

            // Quantity > 0 (не >= 1 — намеренно подчёркиваю положительность).
            // Верхняя граница 999 — защита от случайного "ввёл миллион единиц" из UI.
            // Реальный лимит — сколько B2B даст при reserve, проверится при checkout.
            RuleFor(x => x.Quantity).InclusiveBetween(1, 999);
        }
    }
}
