using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Invoices
{
    public sealed class InvoiceItem :Entity<Guid>
    {
        public Guid InvoiceId { get;private set; }
        public Guid SkuId { get;private set; }


        /// <summary> Заявленно продавцом </summary>
        public int Quantity { get;  private set; }
        /// <summary>Принято оператором. null до приёмки.</summary>
        public int? AcceptedQuantity { get; private set; }



        public InvoiceItem() { }
        public InvoiceItem(Guid id,Guid invoiceId,Guid skuId,int quantity):base(id)
        {
            if (invoiceId == Guid.Empty)
                throw new DomainException("InvoiceId is required", "INVALID_REQUEST");
            if (skuId == Guid.Empty)
                throw new DomainException("SkuId is required", "INVALID_REQUEST");
            if (quantity <= 0)
                throw new DomainException(
                    "quantity must be > 0", "INVALID_REQUEST");
            InvoiceId = invoiceId;
            SkuId = skuId;
            Quantity = quantity;
            AcceptedQuantity = null;
        }
        /// <summary>
        /// Internal: вызывается из Invoice.Accept(). Не публичный.
        /// </summary>
        internal void SetAcceptedQuantity(int accepted)
        {
            if (AcceptedQuantity is not null)
                throw new DomainException(
                    "Item already accepted", "INVALID_STATE_TRANSITION");
            if (accepted < 0)
                throw new DomainException(
                    "accepted_quantity must be >= 0", "INVALID_REQUEST");
            if (accepted > Quantity)
                throw new DomainException(
                    $"accepted_quantity ({accepted}) cannot exceed quantity ({Quantity})",
                    "INVALID_REQUEST");

            AcceptedQuantity = accepted;
        }
    }
}
