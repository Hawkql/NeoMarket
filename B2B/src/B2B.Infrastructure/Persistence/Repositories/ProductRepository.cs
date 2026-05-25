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
            bool includeDeleted,
            int limit,
            int offset,
            CancellationToken ct)
        {
            // Фильтр по seller (IDOR: seller_id из JWT)
            var query = _dbContext.Products
                .AsNoTracking()
                .Where(p => p.SellerId == sellerId);

            // По умолчанию удалённые скрыты, если явно не запрошены
            if (!includeDeleted)
                query = query.Where(p => !p.Deleted);

            if (statusFilter.HasValue)
                query = query.Where(p => p.Status == statusFilter.Value);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<IReadOnlyCollection<Product>> GetPublicByIdsAsync(
    IEnumerable<Guid> productIds, CancellationToken ct)
        {
            var ids = productIds.Distinct().ToArray();
            if (ids.Length == 0)
                return Array.Empty<Product>();

            return await _dbContext.Products
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id)
                            && p.Status == ProductStatus.Moderated
                            && !p.Deleted)
                .ToListAsync(ct);
        }
        public async Task<IReadOnlyCollection<Product>> GetSimilarAsync(
    Guid productId, Guid categoryId, int limit, CancellationToken ct)
        {
            // Случайный порядок — Postgres random(). EF не транслирует Random,
            // поэтому через EF.Functions.Random() (Npgsql поддерживает).
            return await _dbContext.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == categoryId
                            && p.Id != productId
                            && p.Status == ProductStatus.Moderated
                            && !p.Deleted
                            && _dbContext.Skus.Any(s =>
                                s.ProductId == p.Id && !s.Deleted && s.ActiveQuantity > 0))
                .OrderBy(p => EF.Functions.Random())
                .Take(limit)
                .ToListAsync(ct);
        }
        public async Task<(IReadOnlyCollection<Product> Items, int Total)> GetPublicCatalogAsync(
    PublicCatalogFilter filter, CancellationToken ct)
        {
            // Базовый запрос: только витринные товары
            var query = _dbContext.Products
                .AsNoTracking()
                .Where(p => p.Status == ProductStatus.Moderated && !p.Deleted);

            // Категория: либо точная, либо дерево (категория + потомки)
            if (filter.CategoryIdsInTree is { Count: > 0 })
            {
                var treeIds = filter.CategoryIdsInTree.ToArray();
                query = query.Where(p => treeIds.Contains(p.CategoryId));
            }
            else if (filter.CategoryId is not null)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (filter.SellerId is not null)
                query = query.Where(p => p.SellerId == filter.SellerId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = $"%{filter.Search}%";
                query = query.Where(p => EF.Functions.ILike(p.Title, pattern));
            }

            // Товар виден только если есть живой SKU (active_quantity > 0),
            // и (если задан price-фильтр) — SKU в ценовом диапазоне.
            query = query.Where(p =>
                _dbContext.Skus.Any(s =>
                    s.ProductId == p.Id
                    && !s.Deleted
                    && s.ActiveQuantity > 0
                    && (filter.MinPrice == null || s.Price >= filter.MinPrice.Value)
                    && (filter.MaxPrice == null || s.Price <= filter.MaxPrice.Value)));

            var total = await query.CountAsync(ct);

            // Сортировка
            query = filter.Sort switch
            {
                PublicSort.PriceAsc => query.OrderBy(p =>
                    _dbContext.Skus
                        .Where(s => s.ProductId == p.Id && !s.Deleted && s.ActiveQuantity > 0)
                        .Min(s => (int?)s.Price) ?? int.MaxValue),
                PublicSort.PriceDesc => query.OrderByDescending(p =>
                    _dbContext.Skus
                        .Where(s => s.ProductId == p.Id && !s.Deleted && s.ActiveQuantity > 0)
                        .Max(s => (int?)s.Price) ?? 0),
                _ => query.OrderByDescending(p => p.CreatedAt)  // CreatedDesc (+ popular)
            };

            var items = await query
                .Skip(filter.Offset)
                .Take(filter.Limit)
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
