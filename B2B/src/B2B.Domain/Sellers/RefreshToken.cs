using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Sellers
{
    /// <summary>
    /// Хранимый refresh-токен. Храним ХЭШ токена (не plaintext) — при утечке БД
    /// токены нельзя использовать. Ротация: при refresh старый помечается Revoked,
    /// выдаётся новый. Logout — Revoke.
    /// </summary>
    public sealed class RefreshToken : Entity<Guid>
    {
        public Guid SellerId { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTime ExpiresAt { get; private set; }
        public bool Revoked { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private RefreshToken() { }

        public RefreshToken(Guid id, Guid sellerId, string tokenHash,
            DateTime expiresAt, DateTime createdAt) : base(id)
        {
            if (sellerId == Guid.Empty)
                throw new DomainException("SellerId is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new DomainException("tokenHash is required", "INVALID_REQUEST");

            SellerId = sellerId;
            TokenHash = tokenHash;
            ExpiresAt = expiresAt;
            CreatedAt = createdAt;
            Revoked = false;
        }

        public bool IsActive(DateTime now) => !Revoked && ExpiresAt > now;

        public void Revoke() => Revoked = true;
    }
}
