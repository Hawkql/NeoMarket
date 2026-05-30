using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace B2C.Infrastructure.Persistence.Repositories
{
    public sealed class ProductSubscriptionRepository : IProductSubscriptionRepository
    {
        private readonly B2CDbContext _db;

        public ProductSubscriptionRepository(B2CDbContext db) => _db = db;

        public async Task<ProductSubscription?> GetAsync(
            Guid buyerId, Guid productId, CancellationToken ct = default)
            => await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.BuyerId == buyerId && s.ProductId == productId, ct);

        public async Task<bool> ExistsAsync(Guid buyerId, Guid productId, CancellationToken ct = default)
            => await _db.Subscriptions
                .AsNoTracking()
                .AnyAsync(s => s.BuyerId == buyerId && s.ProductId == productId, ct);

        public async Task<IReadOnlyList<ProductSubscription>> ListByBuyerAsync(
            Guid buyerId, CancellationToken ct = default)
            => await _db.Subscriptions
                .Where(s => s.BuyerId == buyerId)
                .ToListAsync(ct);

        public async Task AddAsync(ProductSubscription sub, CancellationToken ct = default)
            => await _db.Subscriptions.AddAsync(sub, ct);

        public void Remove(ProductSubscription sub) => _db.Subscriptions.Remove(sub);
    }
}
