using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Application.Invoices.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Invoices;
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

            // Ownership (US-06): каждый SKU должен принадлежать товару этого продавца
            var ownership = await _skuRepository.GetSellerIdsBySkuIdsAsync(skuIds, ct);

            foreach (var skuId in skuIds)
            {
                if (!ownership.TryGetValue(skuId, out var ownerSellerId))
                    throw new DomainException(
                        $"SKU {skuId} not found", "INVALID_REQUEST");

                if (ownerSellerId != request.SellerId)
                    // Чужой SKU — не раскрываем владельца, отдаём как невалидный запрос
                    throw new DomainException(
                        $"SKU {skuId} does not belong to seller", "FORBIDDEN");
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
