using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;
using B2C.Domain.Orders.Events;

namespace B2C.Domain.Orders
{
    public sealed class Order : AggregateRoot<Guid>, IAuditableEntity
    {
        private readonly List<OrderItem> _items = new();

        // identity
        public Guid BuyerId { get; private set; }
        public IdempotencyKey IdempotencyKey { get; private set; } = null!;

        // content (immutable после создания)
        public DeliveryAddress DeliveryAddress { get; private set; } = null!;
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
        public int TotalAmount { get; private set; }  // сумма копеек

        // lifecycle
        public OrderStatus Status { get; private set; }

        // отслеживание async-операций
        public DateTime? LastUnreserveAttemptAt { get; private set; }
        public DateTime? FulfillCompletedAt { get; private set; }

        // audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Order() { }

        private Order(Guid id, Guid buyerId, IdempotencyKey idempotencyKey,
            DeliveryAddress deliveryAddress) : base(id)
        {
            BuyerId = buyerId;
            IdempotencyKey = idempotencyKey;
            DeliveryAddress = deliveryAddress;
            Status = OrderStatus.Created;
        }

        /// <summary>
        /// Фабрика. ВАЖНО: создаёт заказ в статусе Created. Сразу после успешного reserve
        /// в B2B Application слой вызывает MarkAsPaid() — это атомарная пара (см. канон-flow).
        /// 
        /// items — уже валидированные данные с фиксированными ценами (поход в B2B + проверка
        /// MODERATED/in-stock/quantity сделаны в Application).
        /// </summary>
        public static Order Create(
            Guid buyerId,
            IdempotencyKey idempotencyKey,
            DeliveryAddress deliveryAddress,
            IEnumerable<OrderItemDraft> items)
        {
            if (buyerId == Guid.Empty)
                throw new DomainException("BuyerId is required", "INVALID_REQUEST");
            if (items is null)
                throw new DomainException("items is required", "INVALID_REQUEST");

            var draftList = items.ToList();
            if (draftList.Count == 0)
                throw new DomainException("Список items не может быть пустым", "INVALID_REQUEST");

            var order = new Order(Guid.NewGuid(), buyerId, idempotencyKey, deliveryAddress);

            var total = 0;
            foreach (var d in draftList)
            {
                var item = new OrderItem(
                    Guid.NewGuid(), order.Id,
                    d.SkuId, d.ProductId,
                    d.ProductTitle, d.SkuName,
                    d.Quantity, d.UnitPrice);
                order._items.Add(item);
                total += item.LineTotal;
            }
            order.TotalAmount = total;

            order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, order.BuyerId, order.TotalAmount));
            return order;
        }

        // ===== State machine =====

        /// <summary>Created → Paid. Атомарно после успешного reserve в B2B.</summary>
        public void MarkAsPaid()
        {
            if (Status != OrderStatus.Created)
                throw new DomainException(
                    $"Cannot mark as paid from status {Status}", "CONFLICT");
            Status = OrderStatus.Paid;
        }

        /// <summary>Paid → Assembling. Через Django Admin (operator).</summary>
        public void StartAssembling()
        {
            if (Status != OrderStatus.Paid)
                throw new DomainException(
                    $"Cannot start assembling from status {Status}", "CONFLICT");
            Status = OrderStatus.Assembling;
        }

        /// <summary>Assembling → Delivering.</summary>
        public void StartDelivering()
        {
            if (Status != OrderStatus.Assembling)
                throw new DomainException(
                    $"Cannot start delivering from status {Status}", "CONFLICT");
            Status = OrderStatus.Delivering;
        }

        /// <summary>
        /// Delivering → Delivered (terminal). Поднимает OrderDeliveredEvent —
        /// обработчик в Application вызовет fulfill в B2B (US-ORD-05).
        /// </summary>
        public void MarkAsDelivered()
        {
            if (Status != OrderStatus.Delivering)
                throw new DomainException(
                    $"Cannot mark as delivered from status {Status}", "CONFLICT");
            Status = OrderStatus.Delivered;
            RaiseDomainEvent(new OrderDeliveredEvent(Id, BuyerId));
        }

        /// <summary>
        /// Created/Paid → Cancelled (terminal). Вызывается ПОСЛЕ успешного unreserve в B2B.
        /// US-ORD-03: cancel_paid_order_transitions_to_cancelled.
        /// </summary>
        public void MarkAsCancelled()
        {
            EnsureCancellableStatus();
            Status = OrderStatus.Cancelled;
            RaiseDomainEvent(new OrderCancelledEvent(Id, BuyerId));
        }

        /// <summary>
        /// Created/Paid → CancelPending. Когда unreserve в B2B упал.
        /// Background-job (Infrastructure) будет ретраить (см. RetryUnreserve()).
        /// US-ORD-03: unreserve_failure_transitions_to_cancel_pending.
        /// </summary>
        public void MarkAsCancelPending()
        {
            EnsureCancellableStatus();
            Status = OrderStatus.CancelPending;
            LastUnreserveAttemptAt = DateTime.UtcNow;
            RaiseDomainEvent(new OrderCancelPendingEvent(Id, BuyerId));
        }

        /// <summary>
        /// CancelPending → Cancelled. Вызывается background-job после успешного retry unreserve.
        /// </summary>
        public void CompleteCancelAfterRetry()
        {
            if (Status != OrderStatus.CancelPending)
                throw new DomainException(
                    $"Cannot complete cancel from status {Status}", "CONFLICT");
            Status = OrderStatus.Cancelled;
            RaiseDomainEvent(new OrderCancelledEvent(Id, BuyerId));
        }

        /// <summary>
        /// Зафиксировать неудачную попытку unreserve в статусе CancelPending — для throttling.
        /// Background-job должен пропускать заказы, у которых LastUnreserveAttemptAt
        /// был меньше N минут назад.
        /// </summary>
        public void RecordUnreserveAttempt()
        {
            if (Status != OrderStatus.CancelPending)
                throw new DomainException(
                    $"Cannot record unreserve attempt in status {Status}", "CONFLICT");
            LastUnreserveAttemptAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Зафиксировать, что fulfill в B2B успешно вызван. Идемпотентность для US-ORD-05:
        /// repeated_fulfill_idempotent — повторный вызов с тем же order_id не отправляет
        /// fulfill снова.
        /// </summary>
        public void MarkFulfillCompleted()
        {
            if (Status != OrderStatus.Delivered)
                throw new DomainException(
                    $"Cannot complete fulfill from status {Status}", "CONFLICT");
            if (FulfillCompletedAt is not null) return;  // idempotent
            FulfillCompletedAt = DateTime.UtcNow;
        }

        public bool RequiresFulfill => Status == OrderStatus.Delivered && FulfillCompletedAt is null;



       

        /// <summary>
        /// Можно ли отменить заказ в текущем статусе.
        /// По канон-flow отмена допустима только из Created или Paid
        /// (ASSEMBLING/DELIVERING/DELIVERED — нельзя, US-ORD-03: cancel_assembling_order_returns_409).
        /// 
        /// Публичное свойство, чтобы Application мог проверить статус ДО обращения к B2B unreserve,
        /// не дублируя логику "какие статусы отменяемы". Single source of truth — здесь.
        /// </summary>
        public bool CanBeCancelled => Status is OrderStatus.Created or OrderStatus.Paid;

        // ===== Helpers =====

        private void EnsureCancellableStatus()
        {
            if (!CanBeCancelled)
                throw new DomainException(
                    $"Order in status {Status} cannot be cancelled", "CONFLICT");
        }
    }
}
