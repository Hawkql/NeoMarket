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
    public class Invoice : AggregateRoot<Guid>
    {
        private readonly List<InvoiceLine> _lines = new();

        public Guid SellerId { get; private set; }
        public string Number { get; private set; } = null!;
        public InvoiceStatus Status { get; private set; } 
        public DateTime CreateAt { get; private set; }
        public DateTime AcceptedAt { get; private set; }
        public IReadOnlyList<InvoiceLine> Lines=>_lines.AsReadOnly();
        private Invoice() { }

        public static Invoice Create(Guid sellerId, string number)
        {
            return new Invoice()
            {
                SellerId = sellerId,
                Number = number,
                Status = InvoiceStatus.Draft,
                CreateAt = DateTime.UtcNow,
            };
        }
        public void AddLine(Guid skuId, int quantity,decimal cost)
        {
            if(Status!=InvoiceStatus.Draft)
                throw new DomainException("Cannot modify accepted invoice");
            if(quantity <= 0)
                throw new DomainException("Quantity must be positive");

            var existing = _lines.FirstOrDefault(l=>l.SkuId == skuId);
            if(existing != null)
            {
                existing.IncreaseQuantity(quantity);
                return;
            } 
            _lines.Add(InvoiceLine.Create(Id,skuId,quantity,cost));

        }
        public void Accept()
        {
            if(Status != InvoiceStatus.Draft)
                throw new DomainException("Only draft invoices can be accepted");
            if(!_lines.Any())
                throw new DomainException("Cannot accept empty invoice");
            Status=InvoiceStatus.Accepted;
            AcceptedAt =DateTime.UtcNow;
            AddDomainEvent(new InvoiceAcceptedEvent(
                Id, SellerId, _lines.Select(l => new AcceptedLine(l.SkuId, l.Quantity)).ToList()));
                
        }
    }
}
