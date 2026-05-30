using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Favorites;
using Microsoft.EntityFrameworkCore;

namespace B2C.Infrastructure.Persistence.Repositories
{
    public sealed class FavoriteRepository : IFavoriteRepository
    {
        private readonly B2CDbContext _db;

        public FavoriteRepository(B2CDbContext db) => _db = db;

        public async Task<Favorite?> GetAsync(Guid buyerId, Guid productId, CancellationToken ct = default)
            => await _db.Favorites
                .FirstOrDefaultAsync(f => f.BuyerId == buyerId && f.ProductId == productId, ct);

        public async Task<bool> ExistsAsync(Guid buyerId, Guid productId, CancellationToken ct = default)
            => await _db.Favorites
                .AsNoTracking()
                .AnyAsync(f => f.BuyerId == buyerId && f.ProductId == productId, ct);

        public async Task<IReadOnlyList<Guid>> ListProductIdsByBuyerAsync(
            Guid buyerId, CancellationToken ct = default)
            => await _db.Favorites
                .AsNoTracking()
                .Where(f => f.BuyerId == buyerId)
                .Select(f => f.ProductId)
                .ToListAsync(ct);

        public async Task<IReadOnlyList<Favorite>> ListByBuyerAsync(
            Guid buyerId, CancellationToken ct = default)
            => await _db.Favorites
                .Where(f => f.BuyerId == buyerId)
                .ToListAsync(ct);

        public async Task AddAsync(Favorite favorite, CancellationToken ct = default)
            => await _db.Favorites.AddAsync(favorite, ct);

        public void Remove(Favorite favorite) => _db.Favorites.Remove(favorite);

        /// <summary>
        /// Bulk-удаление по product_id (cross-buyer) при ProductDeleted от B2B.
        /// ExecuteDeleteAsync — один DELETE без загрузки entities.
        /// </summary>
        public async Task RemoveAllByProductAsync(Guid productId, CancellationToken ct = default)
        {
            await _db.Favorites
                .Where(f => f.ProductId == productId)
                .ExecuteDeleteAsync(ct);
        }
    }
}
