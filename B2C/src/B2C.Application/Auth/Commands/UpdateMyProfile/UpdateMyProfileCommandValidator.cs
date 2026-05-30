using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Auth.Commands.UpdateMyProfile
{
    public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
    {
        public UpdateMyProfileCommandValidator()
        {
            RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
            RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
            RuleFor(x => x.Phone).MaximumLength(32).When(x => x.Phone is not null);
        }
    }
}
