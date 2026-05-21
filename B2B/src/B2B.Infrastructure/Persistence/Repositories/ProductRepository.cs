using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence.Repositories
{
    public sealed class ProductRepository : IProductRepository
    {
        private readonly B2BDbContext _dbContext;

        public ProductRepository(B2BDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            // Загружаем агрегат целиком: Product + Characteristics + FieldReports + BlockingReason
            // BlockingReason — Owned Type, грузится автоматически (в той же таблице).
            // ProductCharacteristics — OwnsMany, тоже автоматически.
            // FieldReports — отдельная таблица, нужен явный Include.
            return await _dbContext.Products
                .Include("_fieldReports")        // backing field — приватная коллекция
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<(IReadOnlyCollection<Product> Items, int Total)> GetBySellerAsync(
            Guid sellerId,
            ProductStatus? statusFilter,
            string? searchQuery,
            int limit,
            int offset,
            CancellationToken ct)
        {
            // Базовый запрос — фильтр по seller (IDOR защита: seller_id из JWT)
            var query = _dbContext.Products
                .AsNoTracking()
                .Where(p => p.SellerId == sellerId);

            if (statusFilter.HasValue)
                query = query.Where(p => p.Status == statusFilter.Value);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                // Postgres-specific ILIKE — case-insensitive
                var pattern = $"%{searchQuery}%";
                query = query.Where(p => EF.Functions.ILike(p.Title, pattern));
            }

            // Сначала считаем total — для пагинации
            var total = await query.CountAsync(ct);

            // Затем берём страницу с включёнными коллекциями
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Include("_fieldReports")
                .AsSplitQuery()
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task AddAsync(Product product, CancellationToken ct)
        {
            await _dbContext.Products.AddAsync(product, ct);
        }

        public void Remove(Product product)
        {
            _dbContext.Products.Remove(product);
        }
    }
}
