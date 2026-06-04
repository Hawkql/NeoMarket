using System;
using System.Collections.Generic;
using System.Linq;
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

        // address snapshot (openapi: address объектом)
        public OrderAddress Address { get; private set; } = null!;

        // payment (openapi требует payment_method_id, мы храним id + type)
        public Guid PaymentMethodId { get; private set; }
        public string PaymentMethodType { get; private set; } = "CARD";

        // content
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        // money (openapi: subtotal + delivery_cost = total)
        public int Subtotal { get; private set; }      // copecks, sum(LineTotal)
        public int DeliveryCost { get; private set; }  // MVP: всегда 0
        public int Total => Subtotal + DeliveryCost;   // вычисляемое, в БД не храним

        // optional fields (openapi)
        public string? Comment { get; private set; }
        public string? CancelReason { get; private set; }

        // lifecycle
        public OrderStatus Status { get; private set; }
        public DateTime? PaidAt { get; private set; }
        public DateTime? DeliveredAt { get; private set; }

        // async retry tracking
        public DateTime? LastUnreserveAttemptAt { get; private set; }
        public DateTime? FulfillCompletedAt { get; private set; }

        // audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Order() { }

        private Order(
            Guid id, Guid buyerId, IdempotencyKey idempotencyKey,
            OrderAddress address, Guid paymentMethodId, string paymentMethodType,
            string? comment) : base(id)
        {
            BuyerId = buyerId;
            IdempotencyKey = idempotencyKey;
            Address = address;
            PaymentMethodId = paymentMethodId;
            PaymentMethodType = paymentMethodType;
            Comment = comment;
            Status = OrderStatus.Created;
        }

        /// <summary>
        /// Фабрика. Создаёт заказ в статусе Created. Сразу после успешного reserve
        /// в B2B Application вызывает MarkAsPaid() — атомарная пара (canon-flow).
        /// 
        /// address — snapshot из Buyer.Addresses[address_id] (Application проверил IDOR).
        /// paymentMethodId — Guid из запроса; type на MVP всегда "CARD".
        /// items — валидированные данные с зафиксированными ценами.
        /// </summary>
        public static Order Create(
            Guid buyerId,
            IdempotencyKey idempotencyKey,
            OrderAddress address,
            Guid paymentMethodId,
            string? comment,
            IEnumerable<OrderItemDraft> items,
            string paymentMethodType = "CARD")
        {
            if (buyerId == Guid.Empty)
                throw new DomainException("BuyerId is required", "INVALID_REQUEST");
            if (address is null)
                throw new DomainException("Address is required", "INVALID_REQUEST");
            if (paymentMethodId == Guid.Empty)
                throw new DomainException("PaymentMethodId is required", "INVALID_REQUEST");
            if (items is null)
                throw new DomainException("items is required", "INVALID_REQUEST");

            var draftList = items.ToList();
            if (draftList.Count == 0)
                throw new DomainException("Список items не может быть пустым", "INVALID_REQUEST");

            var order = new Order(
                Guid.NewGuid(), buyerId, idempotencyKey,
                address, paymentMethodId, paymentMethodType, comment);

            var subtotal = 0;
            foreach (var d in draftList)
            {
                var item = new OrderItem(
                    Guid.NewGuid(), order.Id,
                    d.SkuId, d.ProductId,
                    d.ProductTitle, d.SkuName,
                    d.Quantity, d.UnitPrice);
                order._items.Add(item);
                subtotal += item.LineTotal;
            }
            order.Subtotal = subtotal;
            order.DeliveryCost = 0;  // MVP

            order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, order.BuyerId, order.Total));
            return order;
        }

        // ===== State machine =====

        public void MarkAsPaid()
        {
            if (Status != OrderStatus.Created)
                throw new DomainException($"Cannot mark as paid from status {Status}", "CONFLICT");
            Status = OrderStatus.Paid;
            PaidAt = DateTime.UtcNow;
        }

        public void StartAssembling()
        {
            if (Status != OrderStatus.Paid)
                throw new DomainException($"Cannot start assembling from status {Status}", "CONFLICT");
            Status = OrderStatus.Assembling;
        }

        public void StartDelivering()
        {
            if (Status != OrderStatus.Assembling)
                throw new DomainException($"Cannot start delivering from status {Status}", "CONFLICT");
            Status = OrderStatus.Delivering;
        }

        public void MarkAsDelivered()
        {
            if (Status != OrderStatus.Delivering)
                throw new DomainException($"Cannot mark as delivered from status {Status}", "CONFLICT");
            Status = OrderStatus.Delivered;
            DeliveredAt = DateTime.UtcNow;
            RaiseDomainEvent(new OrderDeliveredEvent(Id, BuyerId));
        }

        /// <summary>
        /// Created/Paid → Cancelled. После успешного unreserve в B2B.
        /// reason — опциональная причина (openapi: cancel reason ≤ 500 символов).
        /// </summary>
        public void MarkAsCancelled(string? reason = null)
        {
            EnsureCancellableStatus();
            Status = OrderStatus.Cancelled;
            CancelReason = TrimReason(reason);
            RaiseDomainEvent(new OrderCancelledEvent(Id, BuyerId));
        }

        public void MarkAsCancelPending(string? reason = null)
        {
            EnsureCancellableStatus();
            Status = OrderStatus.CancelPending;
            CancelReason = TrimReason(reason);
            LastUnreserveAttemptAt = DateTime.UtcNow;
            RaiseDomainEvent(new OrderCancelPendingEvent(Id, BuyerId));
        }

        public void CompleteCancelAfterRetry()
        {
            if (Status != OrderStatus.CancelPending)
                throw new DomainException($"Cannot complete cancel from status {Status}", "CONFLICT");
            Status = OrderStatus.Cancelled;
            RaiseDomainEvent(new OrderCancelledEvent(Id, BuyerId));
        }

        public void RecordUnreserveAttempt()
        {
            if (Status != OrderStatus.CancelPending)
                throw new DomainException($"Cannot record unreserve attempt in status {Status}", "CONFLICT");
            LastUnreserveAttemptAt = DateTime.UtcNow;
        }

        public void MarkFulfillCompleted()
        {
            if (Status != OrderStatus.Delivered)
                throw new DomainException($"Cannot complete fulfill from status {Status}", "CONFLICT");
            if (FulfillCompletedAt is not null) return;  // idempotent
            FulfillCompletedAt = DateTime.UtcNow;
        }

        public bool RequiresFulfill => Status == OrderStatus.Delivered && FulfillCompletedAt is null;
        public bool CanBeCancelled => Status is OrderStatus.Created or OrderStatus.Paid;

        private void EnsureCancellableStatus()
        {
            if (!CanBeCancelled)
                throw new DomainException(
                    $"Order in status {Status} cannot be cancelled", "CONFLICT");
        }

        private static string? TrimReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return null;
            // openapi: maxLength 500 — обрезаем на всякий случай, если валидатор пропустил.
            return reason.Length > 500 ? reason.Substring(0, 500) : reason;
        }
    }
}