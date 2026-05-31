using System;
using B2B.Domain.Common;

namespace B2B.Domain.Inventory
{
    /// <summary>
    /// Сохранённая запись резерва для пары (order_id, sku_id).
    /// Используется как источник правды для unreserve:
    /// при снятии берётся quantity из этой записи, а не из тела запроса.
    /// 
    /// Жизненный цикл:
    ///   reserve → INSERT
    ///   unreserve → READ → restore Sku → DELETE
    ///   fulfill → не трогает (fulfill уже принят, идёт по своей идемпотентности по order_id)
    /// </summary>
    public sealed class InventoryReservation : Entity<Guid>
    {
        public Guid OrderId { get; private set; }
        public Guid SkuId { get; private set; }
        public int Quantity { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private InventoryReservation() { }

        public InventoryReservation(Guid id, Guid orderId, Guid skuId, int quantity, DateTime createdAt)
            : base(id)
        {
            if (orderId == Guid.Empty)
                throw new DomainException("OrderId is required", "INVALID_REQUEST");
            if (skuId == Guid.Empty)
                throw new DomainException("SkuId is required", "INVALID_REQUEST");
            if (quantity <= 0)
                throw new DomainException("quantity must be positive", "INVALID_REQUEST");

            OrderId = orderId;
            SkuId = skuId;
            Quantity = quantity;
            CreatedAt = createdAt;
        }
    }
}