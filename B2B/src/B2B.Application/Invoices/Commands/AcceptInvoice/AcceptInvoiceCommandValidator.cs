using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace B2B.Application.Invoices.Commands.AcceptInvoice
{
    public sealed class AcceptInvoiceCommandValidator
    : AbstractValidator<AcceptInvoiceCommand>
    {
        public AcceptInvoiceCommandValidator()
        {
            RuleFor(x => x.InvoiceId).NotEqual(Guid.Empty);

            When(x => x.AcceptedItems is not null, () =>
            {
                RuleForEach(x => x.AcceptedItems!).ChildRules(item =>
                {
                    item.RuleFor(i => i.InvoiceItemId).NotEqual(Guid.Empty);
                    item.RuleFor(i => i.AcceptedQuantity).GreaterThanOrEqualTo(0);
                });
            });
        }
    }
}
