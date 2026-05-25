using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Pagination;
using B2B.Application.Invoices.Dtos;
using B2B.Domain.Invoices;
using MediatR;

namespace B2B.Application.Invoices.Queries.ListInvoices
{

    public sealed record ListInvoicesQuery(
        Guid SellerId,
        InvoiceStatus? Status,
        int Limit,
        int Offset
    ) : IRequest<PagedResult<InvoiceResponseDto>>;
}
