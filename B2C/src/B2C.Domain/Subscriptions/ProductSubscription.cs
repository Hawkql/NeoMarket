using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;
using B2C.Domain.Subscriptions.Events;

namespace B2C.Domain.Subscriptions
{
    public sealed class ProductSubscription : AggregateRoot<Guid>, IAuditableEntity
    {
        public Guid BuyerId { get; private set; }
        public Guid ProductId { get; private set; }
        public NotifyOn NotifyOn { get; private set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private ProductSubscription() { }

        private ProductSubscription(Guid id, Guid buyerId, Guid productId, NotifyOn notifyOn) : base(id)
        {
            BuyerId = buyerId;
            ProductId = productId;
            NotifyOn = notifyOn;
        }

        public static ProductSubscription Create(Guid buyerId, Guid productId, NotifyOn notifyOn)
        {
            if (buyerId == Guid.Empty)
                throw new DomainException("BuyerId is required", "INVALID_REQUEST");
            if (productId == Guid.Empty)
                throw new DomainException("ProductId is required", "INVALID_REQUEST");
            if (notifyOn == NotifyOn.None)
                throw new DomainException("notify_on must contain at least one event type", "INVALID_REQUEST");

            var sub = new ProductSubscription(Guid.NewGuid(), buyerId, productId, notifyOn);
            sub.RaiseDomainEvent(new ProductSubscribedEvent(sub.Id, buyerId, productId, notifyOn));
            return sub;
        }

        public void ChangeNotifyOn(NotifyOn notifyOn)
        {
            if (notifyOn == NotifyOn.None)
                throw new DomainException("notify_on must contain at least one event type", "INVALID_REQUEST");
            NotifyOn = notifyOn;
        }
    }
}
