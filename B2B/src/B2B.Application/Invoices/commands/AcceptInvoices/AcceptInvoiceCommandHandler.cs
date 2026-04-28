using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.common.Interface;
using B2B.Domain.Invoices;
using B2B.Domain.Products;
using MediatR;

namespace B2B.Application.Invoices.commands.AcceptInvoices
{
    public class AcceptInvoiceCommandHandler : IRequestHandler<AcceptInvoiceCommand>
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        public AcceptInvoiceCommandHandler(IInvoiceRepository invoiceRepository, 
            IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _invoiceRepository = invoiceRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(AcceptInvoiceCommand request, CancellationToken ct)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, ct)
                ?? throw new NotFoundException($"Invoice {request.InvoiceId} not found");

            invoice.Accept();
            foreach( var line in invoice.Lines )
            {
                var product = await _productRepository.GetByIdProduct(line.SkuId, ct);
                if( product == null )
                    throw new NotFoundException($"Product {line.SkuId} not found");
                product.IncreaseSkuQuantity(line.SkuId,line.Quantity);
            }
            await _unitOfWork.SaveChangesAsync(ct);
        }
        public class NotFoundException : Exception
        {
            public NotFoundException(string message) : base(message) { }
        }
    }
}
