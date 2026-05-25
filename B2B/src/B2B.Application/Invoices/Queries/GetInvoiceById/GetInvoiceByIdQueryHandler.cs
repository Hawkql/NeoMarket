using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Invoices.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Invoices;
using MediatR;

namespace B2B.Application.Invoices.Queries.GetInvoiceById
{
    public sealed class GetInvoiceByIdQueryHandler
    : IRequestHandler<GetInvoiceByIdQuery, InvoiceResponseDto>
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public GetInvoiceByIdQueryHandler(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<InvoiceResponseDto> Handle(
            GetInvoiceByIdQuery request,
            CancellationToken ct)
        {
            // GetByIdAsync грузит агрегат с Items (Include "_items" в репозитории)
            var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, ct);

            if (invoice is null)
                throw new DomainException("Invoice not found", "NOT_FOUND");

            // Resource hiding: чужой инвойс → 404, не 403
            if (invoice.SellerId != request.SellerId)
                throw new DomainException("Invoice not found", "NOT_FOUND");

            return InvoiceDtoMapper.Map(invoice);
        }
    }
}
