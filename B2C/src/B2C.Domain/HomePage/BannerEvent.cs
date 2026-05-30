using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.HomePage
{
    

        public sealed class BannerEvent : AggregateRoot<Guid>
    {
        public Guid BannerId { get; private set; }
        public Guid? BuyerId { get; private set; }      // null для гостя
        public string? SessionId { get; private set; }  // для гостя
        public BannerEventType Type { get; private set; }
        public DateTime OccurredAt { get; private set; }

        private BannerEvent() { }

        private BannerEvent(Guid id, Guid bannerId, Guid? buyerId, string? sessionId, BannerEventType type)
            : base(id)
        {
            BannerId = bannerId;
            BuyerId = buyerId;
            SessionId = sessionId;
            Type = type;
            OccurredAt = DateTime.UtcNow;
        }

        public static BannerEvent Record(Guid bannerId, Guid? buyerId, string? sessionId, BannerEventType type)
        {
            if (bannerId == Guid.Empty)
                throw new DomainException("BannerId is required", "INVALID_REQUEST");
            return new BannerEvent(Guid.NewGuid(), bannerId, buyerId, sessionId, type);
        }
    }
}
