using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.common.Interface;
using B2B.Domain.Invoices;
using MediatR;

namespace B2B.Application.Invoices.commands.CreateInvoices
{
    public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Guid>
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateInvoiceCommandHandler(
            IInvoiceRepository invoiceRepository,
            IUnitOfWork unitOfWork)
        {
            _invoiceRepository = invoiceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken ct)
        {
            // Создаём накладную через фабричный метод агрегата
            var invoice = Invoice.Create(request.SellerId, request.Number);

            // Добавляем строки через метод агрегата (он же проверит инварианты)
            foreach (var line in request.Lines)
                invoice.AddLine(line.SkuId, line.Quantity, line.Cost);

            await _invoiceRepository.AddAsync(invoice, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return invoice.Id;
        }
    }
}
