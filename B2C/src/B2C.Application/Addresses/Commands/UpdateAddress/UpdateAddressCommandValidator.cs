using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2C.Application.Addresses.Commands.UpdateAddress
{
    public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
    {
        public UpdateAddressCommandValidator()
        {
            RuleFor(x => x.AddressId).NotEmpty();
            RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
            RuleFor(x => x.House).MaximumLength(20).When(x => x.House is not null);
            RuleFor(x => x.Apartment).MaximumLength(20).When(x => x.Apartment is not null);
            RuleFor(x => x.PostalCode).MaximumLength(20).When(x => x.PostalCode is not null);
        }
    }
}
