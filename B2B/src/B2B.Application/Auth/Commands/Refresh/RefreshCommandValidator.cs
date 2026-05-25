using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Auth.Commands.Refresh
{
    public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
    {
        public RefreshCommandValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("refresh_token is required");
        }
    }
}
