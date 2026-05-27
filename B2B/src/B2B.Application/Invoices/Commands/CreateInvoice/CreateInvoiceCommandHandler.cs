using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Application.Invoices.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Invoices;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Invoices.Commands.CreateInvoice
{
    public sealed class CreateInvoiceCommandHandler
    : IRequestHandler<CreateInvoiceCommand, InvoiceResponseDto>
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateInvoiceCommandHandler(
            IInvoiceRepository invoiceRepository,
            ISkuRepository skuRepository,
            IUnitOfWork unitOfWork)
        {
            _invoiceRepository = invoiceRepository;
            _skuRepository = skuRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<InvoiceResponseDto> Handle(
            CreateInvoiceCommand request,
            CancellationToken ct)
        {
            var skuIds = request.Items.Select(i => i.SkuId).Distinct().ToList();

            // US-06: ownership + статус товара одним запросом
            var info = await _skuRepository.GetOwnerAndStatusBySkuIdsAsync(skuIds, ct);

            foreach (var skuId in skuIds)
            {
                if (!info.TryGetValue(skuId, out var data))
                    throw new DomainException("SKU not found", "NOT_FOUND");

                if (data.SellerId != request.SellerId)
                    throw new DomainException(
                        "One or more SKUs do not belong to the authenticated seller", "NOT_OWNER");

                if (data.ProductStatus != ProductStatus.Moderated)
                    throw new DomainException(
                        "Invoice can only be created for MODERATED products", "INVALID_REQUEST");
            }
            // Aggregate Factory: дубли SKU, quantity>0 проверяются внутри Create
            var invoice = Invoice.Create(
                request.SellerId,
                request.Items.Select(i => (i.SkuId, i.Quantity)));

            await _invoiceRepository.AddAsync(invoice, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return InvoiceDtoMapper.Map(invoice);
        }
    }
}
