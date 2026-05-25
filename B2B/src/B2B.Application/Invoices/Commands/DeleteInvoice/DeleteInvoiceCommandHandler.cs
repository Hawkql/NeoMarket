using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Common;
using B2B.Domain.Invoices;
using MediatR;

namespace B2B.Application.Invoices.Commands.DeleteInvoice
{
    public sealed class DeleteInvoiceCommandHandler
    : IRequestHandler<DeleteInvoiceCommand>
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteInvoiceCommandHandler(
            IInvoiceRepository invoiceRepository,
            IUnitOfWork unitOfWork)
        {
            _invoiceRepository = invoiceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteInvoiceCommand request, CancellationToken ct)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, ct);

            if (invoice is null)
                throw new DomainException("Invoice not found", "NOT_FOUND");

            // Resource hiding: чужой инвойс → 404
            if (invoice.SellerId != request.SellerId)
                throw new DomainException("Invoice not found", "NOT_FOUND");

            // Доменный страж: только Created, иначе CONFLICT → 409
            invoice.EnsureCanBeDeleted();

            _invoiceRepository.Remove(invoice);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
