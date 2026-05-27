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
    public class Invoice : AggregateRoot<Guid>, IAuditableEntity
    {
        private readonly List<InvoiceItem> _items = new();

        public Guid SellerId { get; private set; }
        public InvoiceStatus Status { get; private set; }

        /// <summary>Время приёмки. null пока не принята/не отменена без приёмки.</summary>
        public DateTime? AcceptedAt { get; private set; }

        /// <summary>Кто принял накладную (sub из JWT). null до приёмки.</summary>
        public Guid? AcceptedBy { get; private set; }

        public IReadOnlyList<InvoiceItem> Items => _items.AsReadOnly();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Invoice() { }

        private Invoice(Guid id, Guid sellerId) : base(id)
        {
            SellerId = sellerId;
            Status = InvoiceStatus.Pending;
            AcceptedAt = null;
            AcceptedBy = null;
        }

        public static Invoice Create(
            Guid sellerId,
            IEnumerable<(Guid SkuId, int Quantity)> items)
        {
            if (sellerId == Guid.Empty)
                throw new DomainException("SellerId is required", "INVALID_REQUEST");

            var itemsList = items?.ToList()
                ?? throw new DomainException("items are required", "INVALID_REQUEST");

            if (itemsList.Count == 0)
                throw new DomainException("At least one item is required", "INVALID_REQUEST");

            var duplicateSkuIds = itemsList.GroupBy(i => i.SkuId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateSkuIds.Any())
                throw new DomainException(
                    $"Duplicate SKU in invoice: {string.Join(", ", duplicateSkuIds)}",
                    "INVALID_REQUEST");

            var invoice = new Invoice(Guid.NewGuid(), sellerId);

            foreach (var (skuId, quantity) in itemsList)
            {
                if (quantity <= 0)
                    throw new DomainException("quantity must be positive", "INVALID_REQUEST");

                var item = new InvoiceItem(Guid.NewGuid(), invoice.Id, skuId, quantity);
                invoice._items.Add(item);
            }

            invoice.RaiseDomainEvent(new InvoiceCreatedEvent(
                invoice.Id, sellerId, itemsList.Select(i => i.SkuId).ToList()));

            return invoice;
        }

        /// <summary>
        /// Приёмка накладной по invoice_item_id.
        /// 
        /// acceptedByItemId — словарь invoice_item_id → accepted_quantity.
        /// Позиции, НЕ указанные в словаре, принимаются полностью (accepted = quantity).
        /// Пустой словарь → вся накладная принята полностью.
        /// 
        /// Меняет только Invoice. SKU.IncreaseStock вызывается в Handler
        /// в той же транзакции.
        /// </summary>
        public void Accept(
            IReadOnlyDictionary<Guid, int> acceptedByItemId,
            Guid acceptedBy,
            DateTime acceptedAt)
        {
            if (Status != InvoiceStatus.Pending)
                throw new DomainException(
                    $"Cannot accept invoice in status {Status}",
                    "INVALID_STATE_TRANSITION");

            // Проверяем, что все переданные item_id принадлежат накладной
            var presentItemIds = _items.Select(i => i.Id).ToHashSet();
            var unknown = acceptedByItemId.Keys.Where(id => !presentItemIds.Contains(id)).ToList();
            if (unknown.Any())
                throw new DomainException(
                    $"Unknown invoice_item_id: {string.Join(", ", unknown)}",
                    "INVALID_REQUEST");

            // Применяем accepted_quantity: указанные — по словарю, остальные — полностью
            foreach (var item in _items)
            {
                var accepted = acceptedByItemId.TryGetValue(item.Id, out var qty)
                    ? qty
                    : item.Quantity;   // не указан → принят полностью

                item.SetAcceptedQuantity(accepted);  // внутренняя валидация 0..quantity
            }

            Status = ComputeStatus();
            AcceptedAt = acceptedAt;
            AcceptedBy = acceptedBy;

            var acceptedLines = _items
                .Where(i => i.AcceptedQuantity > 0)
                .Select(i => new InvoiceAcceptedLine(i.SkuId, i.AcceptedQuantity!.Value))
                .ToList();

            RaiseDomainEvent(new InvoiceAcceptedEvent(
                Id, SellerId, Status, acceptedLines));
        }
        public void EnsureCanBeDeleted()
        {
            if (Status != InvoiceStatus.Pending)
                throw new DomainException(
                    $"Cannot delete invoice in status {Status}", "CONFLICT");
        }
        private InvoiceStatus ComputeStatus()
        {
            var allFullyAccepted = _items.All(i => i.AcceptedQuantity == i.Quantity);
            var allRejected = _items.All(i => i.AcceptedQuantity == 0);

            return (allFullyAccepted, allRejected) switch
            {
                (true, _) => InvoiceStatus.Accepted,
                (_, true) => InvoiceStatus.Cancelled,
                _ => InvoiceStatus.PartiallyAccepted
            };
        }
    }
}

