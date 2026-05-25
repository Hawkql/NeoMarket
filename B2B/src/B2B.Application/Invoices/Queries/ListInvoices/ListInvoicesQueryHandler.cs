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
    public sealed class ListInvoicesQueryHandler
    : IRequestHandler<ListInvoicesQuery, PagedResult<InvoiceResponseDto>>
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public ListInvoicesQueryHandler(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<PagedResult<InvoiceResponseDto>> Handle(
            ListInvoicesQuery request,
            CancellationToken ct)
        {
            // Фильтр по SellerId — внутри репозитория (IDOR: продавец видит только свои)
            var (invoices, total) = await _invoiceRepository.GetBySellerAsync(
                request.SellerId,
                request.Status,
                request.Limit,
                request.Offset,
                ct);

            var items = invoices
                .Select(InvoiceDtoMapper.Map)
                .ToList();

            return new PagedResult<InvoiceResponseDto>(
                Items: items,
                TotalCount: total,
                Limit: request.Limit,
                Offset: request.Offset);
        }
    }
}
