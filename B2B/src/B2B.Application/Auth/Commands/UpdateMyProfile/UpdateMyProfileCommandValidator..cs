using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Auth.Commands.UpdateMyProfile
{
    public sealed class UpdateMyProfileCommandValidator
    : AbstractValidator<UpdateMyProfileCommand>
    {
        public UpdateMyProfileCommandValidator()
        {
            When(x => x.FirstName is not null, () =>
                RuleFor(x => x.FirstName!).NotEmpty().MaximumLength(100));

            When(x => x.LastName is not null, () =>
                RuleFor(x => x.LastName!).NotEmpty().MaximumLength(100));

            When(x => x.CompanyName is not null, () =>
                RuleFor(x => x.CompanyName!).NotEmpty().MaximumLength(255));
        }
    }
}
