using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Skus;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence.Repositories
{
    public sealed class SkuRepository : ISkuRepository
    {
        private readonly B2BDbContext _dbContext;

        public SkuRepository(B2BDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Sku?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            // Обычный read без блокировки. Tracking нужен для последующего изменения.
            return await _dbContext.Skus
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<Sku?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
        {
            // SELECT ... FOR UPDATE — блокирует строку до конца транзакции.
            // Postgres-specific. Если транзакция не открыта — Postgres неявно создаст
            // одну на этот запрос и сразу её закроет (бесполезно).
            // Поэтому Application Handler ОБЯЗАН открыть IDbContextTransaction до этого вызова.
            return await _dbContext.Skus
                .FromSql($"SELECT * FROM skus WHERE id = {id} FOR UPDATE")
                .FirstOrDefaultAsync(ct);
        }
        public async Task<IReadOnlyDictionary<Guid, Guid>> GetSellerIdsBySkuIdsAsync(
    IEnumerable<Guid> skuIds, CancellationToken ct)
        {
            var idArray = skuIds.ToArray();
            if (idArray.Length == 0)
                return new Dictionary<Guid, Guid>();

            // skus.product_id → products.seller_id. Join по разным агрегатам в read-запросе
            // допустим (это query-сторона, не нарушает write-границы).
            var rows = await (
                from sku in _dbContext.Skus.AsNoTracking()
                where idArray.Contains(sku.Id) && !sku.Deleted
                join product in _dbContext.Products.AsNoTracking()
                    on sku.ProductId equals product.Id
                select new { SkuId = sku.Id, product.SellerId })
                .ToListAsync(ct);

            return rows.ToDictionary(r => r.SkuId, r => r.SellerId);
        }
        public async Task<IReadOnlyCollection<Sku>> GetByIdsForUpdateAsync(
            IEnumerable<Guid> ids,
            CancellationToken ct)
        {
            // Batch FOR UPDATE — все SKU одним запросом.
            // КРИТИЧНО: блокируем СРАЗУ ВСЕ строки → нет deadlock'а из-за разного порядка.
            var idArray = ids.ToArray();

            return await _dbContext.Skus
                .FromSql($"SELECT * FROM skus WHERE id = ANY({idArray}) FOR UPDATE")
                .ToListAsync(ct);
        }
        public async Task<IReadOnlyDictionary<Guid, int>> GetMinPriceByProductIdsAsync(
    IEnumerable<Guid> productIds, CancellationToken ct)
        {
            var idArray = productIds.ToArray();
            if (idArray.Length == 0)
                return new Dictionary<Guid, int>();

            // GROUP BY product_id, MIN(price) — один запрос, только не удалённые SKU
            var rows = await _dbContext.Skus
                .AsNoTracking()
                .Where(s => idArray.Contains(s.ProductId) && !s.Deleted)
                .GroupBy(s => s.ProductId)
                .Select(g => new { ProductId = g.Key, MinPrice = g.Min(x => x.Price) })
                .ToListAsync(ct);

            return rows.ToDictionary(r => r.ProductId, r => r.MinPrice);
        }
        public async Task<IReadOnlyCollection<Sku>> GetByProductIdAsync(
            Guid productId,
            CancellationToken ct)
        {
            return await _dbContext.Skus
                .Where(s => s.ProductId == productId && !s.Deleted)
                .ToListAsync(ct);
        }

        public async Task<int> CountByProductIdAsync(Guid productId, CancellationToken ct)
        {
            return await _dbContext.Skus
                .CountAsync(s => s.ProductId == productId && !s.Deleted, ct);
        }

        public async Task AddAsync(Sku sku, CancellationToken ct)
        {
            await _dbContext.Skus.AddAsync(sku, ct);
        }

        public void Remove(Sku sku)
        {
            _dbContext.Skus.Remove(sku);
        }
        public async Task<IReadOnlyCollection<Sku>> GetByProductIdsAsync(
    IEnumerable<Guid> productIds, CancellationToken ct)
        {
            var ids = productIds.Distinct().ToArray();
            if (ids.Length == 0)
                return Array.Empty<Sku>();

            return await _dbContext.Skus
                .AsNoTracking()
                .Where(s => ids.Contains(s.ProductId) && !s.Deleted)
                .ToListAsync(ct);
        }
    }
}
