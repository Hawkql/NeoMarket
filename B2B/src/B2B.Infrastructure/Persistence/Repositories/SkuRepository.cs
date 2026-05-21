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
    }
}
