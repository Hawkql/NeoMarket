using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Carts
{
    public sealed class CartOwner : IEquatable<CartOwner>
    {
        public Guid? BuyerId { get; private set; }
        public string? SessionId { get; private set; }

        public bool IsAuthenticated => BuyerId.HasValue;
        public bool IsGuest => SessionId is not null;

        // Параметрless для EF.
        private CartOwner() { }

        private CartOwner(Guid? buyerId, string? sessionId)
        {
            BuyerId = buyerId;
            SessionId = sessionId;
        }

        public static CartOwner ForBuyer(Guid buyerId)
        {
            if (buyerId == Guid.Empty)
                throw new DomainException("BuyerId is required", "INVALID_REQUEST");
            return new CartOwner(buyerId, null);
        }

        public static CartOwner ForGuest(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new DomainException("SessionId is required", "INVALID_REQUEST");
            if (sessionId.Length > 128)
                throw new DomainException("SessionId too long", "INVALID_REQUEST");
            return new CartOwner(null, sessionId);
        }

        public bool Equals(CartOwner? other)
        {
            if (other is null) return false;
            return BuyerId == other.BuyerId && SessionId == other.SessionId;
        }

        public override bool Equals(object? obj) => Equals(obj as CartOwner);
        public override int GetHashCode() => HashCode.Combine(BuyerId, SessionId);
    }
}
