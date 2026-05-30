using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace B2C.Infrastructure.Persistence.Repositories
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly B2CDbContext _db;

        public OrderRepository(B2CDbContext db) => _db = db;

        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct)
            => await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

        /// <summary>
        /// IDOR-safe: фильтр по обоим Id одним WHERE. Чужой заказ → null → 404 (не 403).
        /// Снаружи различить "не существует" и "чужой" невозможно.
        /// </summary>
        public async Task<Order?> GetByIdForBuyerAsync(Guid orderId, Guid buyerId, CancellationToken ct)
            => await _db.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == buyerId, ct);

        /// <summary>
        /// Idempotency-проверка checkout. Сравнение по VO IdempotencyKey —
        /// EF транслирует через value-конвертер (VO → Guid в SQL).
        /// </summary>
        public async Task<Order?> GetByIdempotencyKeyAsync(
            Guid buyerId, IdempotencyKey key, CancellationToken ct)
            => await _db.Orders
                .FirstOrDefaultAsync(o => o.BuyerId == buyerId && o.IdempotencyKey == key, ct);

        public async Task<(IReadOnlyList<Order> Items, int TotalCount)> ListByBuyerAsync(
            Guid buyerId, OrderStatus? statusFilter, int limit, int offset, CancellationToken ct)
        {
            var query = _db.Orders.AsNoTracking().Where(o => o.BuyerId == buyerId);

            if (statusFilter.HasValue)
                query = query.Where(o => o.Status == statusFilter.Value);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<IReadOnlyList<Order>> ListPendingCancelOlderThanAsync(
            DateTime threshold, int limit, CancellationToken ct)
            => await _db.Orders
                .Where(o => o.Status == OrderStatus.CancelPending
                    && (o.LastUnreserveAttemptAt == null || o.LastUnreserveAttemptAt < threshold))
                .OrderBy(o => o.LastUnreserveAttemptAt)
                .Take(limit)
                .ToListAsync(ct);

        public async Task<IReadOnlyList<Order>> ListPendingFulfillAsync(int limit, CancellationToken ct)
            => await _db.Orders
                .Where(o => o.Status == OrderStatus.Delivered && o.FulfillCompletedAt == null)
                .OrderBy(o => o.UpdatedAt)
                .Take(limit)
                .ToListAsync(ct);

        public async Task AddAsync(Order order, CancellationToken ct)
            => await _db.Orders.AddAsync(order, ct);

        public void Update(Order order) => _db.Orders.Update(order);
    }
}
