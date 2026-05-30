using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Carts
{
    public sealed class CartItem : Entity<Guid>
    {
        public Guid CartId { get; private set; }
        public Guid SkuId { get; private set; }
        public Guid ProductId { get; private set; }
        public int Quantity { get; private set; }
        public UnavailableReason UnavailableReason { get; private set; }
        public DateTime AddedAt { get; private set; }

        public bool IsAvailable => UnavailableReason == UnavailableReason.None;

        private CartItem() { }

        // internal — создаётся только из Cart.AddItem.
        internal CartItem(Guid id, Guid cartId, Guid skuId, Guid productId, int quantity) : base(id)
        {
            CartId = cartId;
            SkuId = skuId;
            ProductId = productId;
            Quantity = quantity;
            UnavailableReason = UnavailableReason.None;
            AddedAt = DateTime.UtcNow;
        }

        internal void IncreaseQuantity(int delta)
        {
            if (delta < 1)
                throw new DomainException("Quantity delta must be >= 1", "INVALID_REQUEST");
            Quantity += delta;
        }

        internal void SetQuantity(int quantity)
        {
            if (quantity < 1)
                throw new DomainException("Quantity must be >= 1", "INVALID_REQUEST");
            Quantity = quantity;
        }

        internal void MarkUnavailable(UnavailableReason reason)
        {
            if (reason == UnavailableReason.None)
                throw new DomainException("Reason must be specified", "INVALID_REQUEST");
            UnavailableReason = reason;
        }

        internal void MarkAvailable() => UnavailableReason = UnavailableReason.None;
    }
}
