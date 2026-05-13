using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;
using B2B.Domain.Invoices.Events;
using static B2B.Domain.Invoices.Events.InvoiceAcceptedEvent;

namespace B2B.Domain.Invoices
{
    public class Invoice : AggregateRoot<Guid>,IAuditableEntity
    {
        private readonly List<InvoiceItem> _items = new();

        public Guid SellerId { get; private set; }
        
        public InvoiceStatus Status { get; private set; } 

        /// <summary>Время приёмки. null пока не принята.</summary>
        public DateTime? AcceptedAt { get; private set; }

        public IReadOnlyList<InvoiceItem> Lines=>_items.AsReadOnly();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Invoice() { }
        private Invoice(Guid id,Guid sellerId):base(id) 
        {
            SellerId = sellerId;
            Status = InvoiceStatus.Pending;
            AcceptedAt = null;
        }
        public static Invoice Create(Guid sellerId,
            IEnumerable<(Guid SkuId,int Quantity)> items)
        {
            if (sellerId == Guid.Empty)
                throw new DomainException("SellerId is required", "INVALID_REQUEST");
            var itemsList = items?.ToList()
           ?? throw new DomainException("items are required", "INVALID_REQUEST");

            if (itemsList.Count == 0)
                throw new DomainException(
                    "At least one item is required", "INVALID_REQUEST");

            var dublicateSkuIds = itemsList.GroupBy(i=>i.SkuId)
                .Where(i=>i.Count()>1)
                .Select(k=>k.Key)
                .ToList();
            if(dublicateSkuIds.Any())
                throw new DomainException(
                $"Duplicate SKU in invoice: {string.Join(", ", dublicateSkuIds)}",
                "INVALID_REQUEST");

            var invoice = new Invoice(Guid.NewGuid(),sellerId);


            foreach( var (skuid ,quantity) in itemsList)
            {
                var item = new InvoiceItem(
                    Guid.NewGuid(), invoice.Id, skuid, quantity);
                invoice._items.Add(item);

            }

            invoice.RaiseDomainEvent(new InvoiceCreatedEvent(invoice.Id, sellerId,
                itemsList.Select(i => i.SkuId).ToList()));

            return invoice;
        }
        /// <summary>
        /// Приёмка накладной. На входе — словарь sku_id → accepted_quantity.
        /// 
        /// Меняет ТОЛЬКО Invoice (status, accepted_at, items.accepted_quantity).
        /// SKU.ActiveQuantity увеличивается отдельно в Application Handler
        /// в той же транзакции.
        /// 
        /// После приёмки изменения невозможны.
        /// </summary>
        public void Accept(
            IReadOnlyDictionary<Guid, int> acceptedBySkuId,
            DateTime acceptedAt)
        {
            if (Status != InvoiceStatus.Pending)
                throw new DomainException(
                    $"Cannot accept invoice in status {Status}",
                    "INVALID_STATE_TRANSITION");

            // Проверяем, что все items получили решение
            var presentSkuIds = _items.Select(i => i.SkuId).ToHashSet();
            var providedSkuIds = acceptedBySkuId.Keys.ToHashSet();

            if (!presentSkuIds.SetEquals(providedSkuIds))
            {
                var missing = presentSkuIds.Except(providedSkuIds);
                var extra = providedSkuIds.Except(presentSkuIds);
                throw new DomainException(
                    $"Acceptance must cover all items exactly. " +
                    $"Missing: [{string.Join(", ", missing)}], " +
                    $"Extra: [{string.Join(", ", extra)}]",
                    "INVALID_REQUEST");
            }

            // Применяем accepted_quantity к каждой позиции
            foreach (var item in _items)
            {
                item.SetAcceptedQuantity(acceptedBySkuId[item.SkuId]);
            }

            // Вычисляем финальный статус
            Status = ComputeStatus();
            AcceptedAt = acceptedAt;

            // Готовим payload для события (только принятые позиции)
            var acceptedLines = _items
                .Where(i => i.AcceptedQuantity > 0)
                .Select(i => new InvoiceAcceptedLine(i.SkuId, i.AcceptedQuantity!.Value))
                .ToList();

            RaiseDomainEvent(new InvoiceAcceptedEvent(
                Id, SellerId, Status, acceptedLines));
        }

        private InvoiceStatus ComputeStatus()
        {
            var allFullyAccepted = _items.All(
                i => i.AcceptedQuantity == i.Quantity);
            var allRejected = _items.All(
                i => i.AcceptedQuantity == 0);

            return (allFullyAccepted, allRejected) switch
            {
                (true, _) => InvoiceStatus.Accepted,
                (_, true) => InvoiceStatus.Rejected,
                _ => InvoiceStatus.PartiallyAccepted
            };
        }
    }
}
