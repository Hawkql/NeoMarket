using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Invoices.Dtos;
using MediatR;

namespace B2B.Application.Invoices.Commands.AcceptInvoice
{
    public sealed record AcceptInvoiceCommand(
        Guid InvoiceId,
        Guid AcceptedBy,
        IReadOnlyList<AcceptInvoiceItemInputDto>? AcceptedItems  // null/пусто → принять всё полностью
    ) : IRequest<InvoiceResponseDto>;
}
