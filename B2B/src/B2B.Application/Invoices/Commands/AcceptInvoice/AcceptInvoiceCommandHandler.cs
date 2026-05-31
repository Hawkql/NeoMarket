using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Application.Common.Interface;
using B2B.Application.Invoices.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Invoices;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Invoices.Commands.AcceptInvoice
{
    public sealed class AcceptInvoiceCommandHandler
    : IRequestHandler<AcceptInvoiceCommand, InvoiceResponseDto>
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly ITransactionManager _transactionManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public AcceptInvoiceCommandHandler(
            IInvoiceRepository invoiceRepository,
            ISkuRepository skuRepository,
            ITransactionManager transactionManager,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock)
        {
            _invoiceRepository = invoiceRepository;
            _skuRepository = skuRepository;
            _transactionManager = transactionManager;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<InvoiceResponseDto> Handle(
            AcceptInvoiceCommand request,
            CancellationToken ct)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, ct);

            if (invoice is null)
                throw new DomainException("Invoice not found", "NOT_FOUND");

            if (invoice.Status != InvoiceStatus.Created)              
                throw new DomainException(
                    $"Cannot accept invoice in status {invoice.Status}", "CONFLICT");

            // Словарь invoice_item_id → accepted_quantity (пусто = принять всё полностью)
            var acceptedByItemId = (request.AcceptedItems ?? new List<AcceptInvoiceItemInputDto>())
                .ToDictionary(i => i.InvoiceItemId, i => i.AcceptedQuantity);

            // SKU, остатки которых будем увеличивать
            var skuIds = invoice.Items.Select(i => i.SkuId).Distinct().ToList();

            await _transactionManager.BeginAsync(ct);
            try
            {
                // Лочим SKU перед изменением остатков (защита от гонок с reserve)
                var skus = await _skuRepository.GetByIdsForUpdateAsync(skuIds, ct);
                var skuMap = skus.ToDictionary(s => s.Id);

                // 1. Приёмка инвойса (валидация item_id, расчёт статуса, событие)
                invoice.Accept(acceptedByItemId, request.AcceptedBy, _clock.UtcNow);

                // 2. Увеличиваем остатки SKU на принятое количество (orchestration)
                foreach (var item in invoice.Items)
                {
                    var accepted = item.AcceptedQuantity ?? 0;
                    if (accepted <= 0)
                        continue;

                    if (!skuMap.TryGetValue(item.SkuId, out var sku) || sku.Deleted)
                        // SKU удалён к моменту приёмки — принять его нельзя
                        throw new DomainException(
                            $"SKU {item.SkuId} not found or deleted", "CONFLICT");

                    sku.IncreaseStock(accepted);
                }

                // 3. Invoice + Sku + Outbox в одной транзакции
                await _unitOfWork.SaveChangesAsync(ct);
                await _transactionManager.CommitAsync(ct);

                return InvoiceDtoMapper.Map(invoice);
            }
            catch
            {
                await _transactionManager.RollbackAsync(ct);
                throw;
            }
        }
    }
}
