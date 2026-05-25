using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Application.Invoices.Commands.DeleteInvoice
{
    public sealed record DeleteInvoiceCommand(
        Guid InvoiceId,
        Guid SellerId
    ) : IRequest;
}
