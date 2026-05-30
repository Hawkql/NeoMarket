using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Auth.Commands.Register
{
    public sealed class BuyerRegisterCommandValidator : AbstractValidator<BuyerRegisterCommand>
    {
        public BuyerRegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128)
                .Matches(@"[A-Za-z]").WithMessage("Password must contain a letter")
                .Matches(@"\d").WithMessage("Password must contain a digit");

            RuleFor(x => x.FirstName).MaximumLength(100);
            RuleFor(x => x.LastName).MaximumLength(100);
            RuleFor(x => x.Phone).MaximumLength(32);
        }
    }
}
