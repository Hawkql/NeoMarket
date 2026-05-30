using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Buyers;
using Microsoft.EntityFrameworkCore;

namespace B2C.Infrastructure.Persistence.Repositories
{
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly B2CDbContext _db;

        public RefreshTokenRepository(B2CDbContext db) => _db = db;

        public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct)
            => await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        public async Task AddAsync(RefreshToken token, CancellationToken ct)
            => await _db.RefreshTokens.AddAsync(token, ct);

        /// <summary>
        /// Bulk-отзыв всех активных токенов покупателя. ExecuteUpdateAsync — EF Core 7+
        /// bulk-update без загрузки entities в память (один UPDATE-запрос).
        /// 
        /// ВАЖНО: ExecuteUpdateAsync выполняется НЕМЕДЛЕННО, в обход ChangeTracker и
        /// вне обычного SaveChanges. Внутри транзакции (TransactionBehavior) это ок —
        /// UPDATE идёт в той же транзакции, что и остальные изменения команды.
        /// </summary>
        public async Task RevokeAllForBuyerAsync(Guid buyerId, CancellationToken ct)
        {
            await _db.RefreshTokens
                .Where(t => t.BuyerId == buyerId && !t.Revoked)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revoked, true), ct);
        }
    }
}
