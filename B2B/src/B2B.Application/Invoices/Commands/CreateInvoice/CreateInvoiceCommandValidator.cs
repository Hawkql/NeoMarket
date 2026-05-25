using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Invoices.Commands.CreateInvoice
{
    public sealed class CreateInvoiceCommandValidator
    : AbstractValidator<CreateInvoiceCommand>
    {
        public CreateInvoiceCommandValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("items must contain at least one entry");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.SkuId).NotEqual(Guid.Empty);
                item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(1);
            });
        }
    }
}
