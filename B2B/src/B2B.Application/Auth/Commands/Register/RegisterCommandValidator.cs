using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Auth.Commands.Register
{
    public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().EmailAddress().WithMessage("valid email is required");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8).WithMessage("password must be 8-128 characters")
                .MaximumLength(128);

            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.CompanyName).NotEmpty();

            RuleFor(x => x.Inn)
                .NotEmpty()
                .Length(10, 12).WithMessage("inn must be 10-12 characters")
                .Matches(@"^\d+$").WithMessage("inn must contain only digits");
        }
    }
}
