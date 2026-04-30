using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Invoices.commands.CreateInvoices
{
    public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
    {
        public CreateInvoiceCommandValidator()
        {
            RuleFor(x => x.SellerId).NotEmpty();
            RuleFor(x => x.Number)
                .NotEmpty()
                .MaximumLength(50);
            RuleFor(x => x.Lines)
                .NotEmpty().WithMessage("Invoice must contain at least one line");
            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.SkuId).NotEmpty();
                line.RuleFor(l => l.Quantity).GreaterThan(0);
                line.RuleFor(l => l.Cost).GreaterThan(0);
            });
        }
    }
}
