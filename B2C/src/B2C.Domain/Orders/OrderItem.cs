using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Orders
{
    public sealed class OrderItem : Entity<Guid>
    {
        public Guid OrderId { get; private set; }
        public Guid SkuId { get; private set; }
        public Guid ProductId { get; private set; }

        // Snapshot fields — никогда не изменяются после создания.
        public string ProductTitle { get; private set; } = null!;
        public string SkuName { get; private set; } = null!;
        public int Quantity { get; private set; }
        public int UnitPrice { get; private set; }  // копейки

        public int LineTotal => UnitPrice * Quantity;

        private OrderItem() { }

        internal OrderItem(Guid id, Guid orderId, Guid skuId, Guid productId,
            string productTitle, string skuName, int quantity, int unitPrice) : base(id)
        {
            if (skuId == Guid.Empty)
                throw new DomainException("SkuId is required", "INVALID_REQUEST");
            if (productId == Guid.Empty)
                throw new DomainException("ProductId is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(productTitle))
                throw new DomainException("ProductTitle is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(skuName))
                throw new DomainException("SkuName is required", "INVALID_REQUEST");
            if (quantity < 1)
                throw new DomainException("Quantity must be >= 1", "INVALID_REQUEST");
            if (unitPrice < 0)
                throw new DomainException("UnitPrice must be >= 0", "INVALID_REQUEST");

            OrderId = orderId;
            SkuId = skuId;
            ProductId = productId;
            ProductTitle = productTitle;
            SkuName = skuName;
            Quantity = quantity;
            UnitPrice = unitPrice;
        }
    }
}
