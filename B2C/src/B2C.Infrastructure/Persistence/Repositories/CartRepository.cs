using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Carts;
using Microsoft.EntityFrameworkCore;

namespace B2C.Infrastructure.Persistence.Repositories
{
    public sealed class CartRepository : ICartRepository
    {
        private readonly B2CDbContext _db;

        public CartRepository(B2CDbContext db) => _db = db;

        // Owned-коллекция Items подгружается автоматически (EF Core всегда включает owned types).
        public async Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken ct)
            => await _db.Carts.FirstOrDefaultAsync(c => c.Id == cartId, ct);

        public async Task<Cart?> GetByBuyerAsync(Guid buyerId, CancellationToken ct)
            => await _db.Carts.FirstOrDefaultAsync(c => c.Owner.BuyerId == buyerId, ct);

        public async Task<Cart?> GetBySessionAsync(string sessionId, CancellationToken ct)
            => await _db.Carts.FirstOrDefaultAsync(c => c.Owner.SessionId == sessionId, ct);

        /// <summary>
        /// Корзины, содержащие любой из указанных SKU (для SKU_OUT_OF_STOCK от B2B).
        /// Запрос по owned-коллекции Items: EF транслирует в JOIN + EXISTS.
        /// </summary>
        public async Task<IReadOnlyList<Cart>> ListWithAnySkuAsync(
            IEnumerable<Guid> skuIds, CancellationToken ct)
        {
            var ids = new HashSet<Guid>(skuIds);
            return await _db.Carts
                .Where(c => c.Items.Any(i => ids.Contains(i.SkuId)))
                .ToListAsync(ct);
        }

        /// <summary>
        /// Корзины, содержащие товар (для PRODUCT_BLOCKED/PRODUCT_DELETED от B2B).
        /// </summary>
        public async Task<IReadOnlyList<Cart>> ListWithProductAsync(Guid productId, CancellationToken ct)
            => await _db.Carts
                .Where(c => c.Items.Any(i => i.ProductId == productId))
                .ToListAsync(ct);

        public async Task AddAsync(Cart cart, CancellationToken ct)
            => await _db.Carts.AddAsync(cart, ct);

        public void Update(Cart cart) => _db.Carts.Update(cart);

        public void Remove(Cart cart) => _db.Carts.Remove(cart);
    }
}
