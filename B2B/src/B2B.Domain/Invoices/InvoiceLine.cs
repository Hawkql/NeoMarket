using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Invoices
{
    public class InvoiceLine:Entity<Guid>
    {
        public Guid InvoiceId { get; private set; }
        public Guid SkuId { get; private set; }
        public int Quantity { get; private set; }
        public decimal Cost {  get; private set; }
        private InvoiceLine() { }

        public static InvoiceLine Create(Guid invoiceId,
            Guid skuId, 
            int quantity,
            decimal cost)
        {
            return new InvoiceLine()
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                SkuId = skuId,
                Quantity = quantity,
                Cost = cost
            };
        }
        public void IncreaseQuantity(int amount) => Quantity += amount;
    }
}
