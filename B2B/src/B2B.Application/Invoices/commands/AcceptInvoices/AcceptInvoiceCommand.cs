using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Application.Invoices.commands.AcceptInvoices
{
    public record AcceptInvoiceCommand(Guid InvoiceId):IRequest;

}
