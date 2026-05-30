using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Buyers
{

    /// <summary>
    /// Refresh-токен покупателя. Внутренняя entity агрегата Buyer:
    /// существует только в контексте своего владельца, отдельным репозиторием не загружается.
    /// 
    /// JWT rotation: каждый раз при /auth/refresh выдаётся новая пара access+refresh,
    /// старый refresh помечается Revoked. Это защита от replay-атак.
    /// </summary>
    public sealed class RefreshToken : Entity<Guid>
    {
        public Guid BuyerId { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTime ExpiresAt { get; private set; }
        public bool Revoked { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private RefreshToken() { }

        public RefreshToken(Guid id, Guid buyerId, string tokenHash,
            DateTime expiresAt, DateTime createdAt) : base(id)
        {
            if (buyerId == Guid.Empty)
                throw new DomainException("BuyerId is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new DomainException("tokenHash is required", "INVALID_REQUEST");

            BuyerId = buyerId;
            TokenHash = tokenHash;
            ExpiresAt = expiresAt;
            CreatedAt = createdAt;
            Revoked = false;
        }

        public bool IsActive(DateTime now) => !Revoked && ExpiresAt > now;

        public void Revoke() => Revoked = true;
    }
}
