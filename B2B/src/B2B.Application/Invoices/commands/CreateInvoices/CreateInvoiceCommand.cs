using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Invoices;
using MediatR;

namespace B2B.Application.Invoices.commands.CreateInvoices
{
    public record CreateInvoiceCommand(Guid SellerId,
             string Number, List<InvoiceLine> Lines) : IRequest<Guid>
    { }
   
    public record InvoiceLineDto(Guid SkuId, int Quantity, decimal Cost);
}
