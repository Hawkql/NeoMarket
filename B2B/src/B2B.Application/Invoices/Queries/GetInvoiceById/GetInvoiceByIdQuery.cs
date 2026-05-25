using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Invoices.Dtos;
using MediatR;

namespace B2B.Application.Invoices.Queries.GetInvoiceById
{
    public sealed record GetInvoiceByIdQuery(
        Guid InvoiceId,
        Guid SellerId
    ) : IRequest<InvoiceResponseDto>;
}
