using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Domain.Repository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repository
{
    /// <summary>
    /// Репозиторий товаров. Все запросы AsNoTracking — сервис read-only.
    /// Full-text search через PostgreSQL tsvector / plainto_tsquery.
    /// </summary>

    /// <summary>
    /// Репозиторий товаров. Все запросы AsNoTracking — сервис read-only.
    /// Full-text search через PostgreSQL tsvector / plainto_tsquery.
    /// </summary>
    public sealed class ProductRepository(CatalogDbContext db) : IProductRepository
    {
        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await db.Products
                .AsNoTracking()
                .Include(p => p.Images.OrderBy(i => i.Order))
                .Include(p => p.Characteristics)
                .Include(p => p.Skus)
                    .ThenInclude(s => s.Characteristics)
                .Include(p => p.Skus)
                    .ThenInclude(s => s.Images.OrderBy(i => i.Order))
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<(List<Product> Items, int TotalCount)> ListAsync(
            Guid? categoryId,
            string? search,
            Dictionary<string, string>? filters,
            string? sort,
            int limit,
            int offset,
            CancellationToken ct = default)
        {
            var query = db.Products
                .AsNoTracking()
                .Where(p => p.Status == ProductStatus.Moderated);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    EF.Functions.ToTsVector("russian", p.Title + " " + p.Description)
                        .Matches(EF.Functions.PlainToTsQuery("russian", term)));
            }

            if (filters is { Count: > 0 })
            {
                foreach (var (key, value) in filters)
                {
                    var k = key.ToLower();
                    var v = value.ToLower();
                    query = query.Where(p =>
                        db.Characteristics.Any(c =>
                            c.ProductId == p.Id &&
                            c.Name.ToLower() == k &&
                            c.Value.ToLower() == v));
                }
            }

            query = sort switch
            {
                "price_asc" => query.OrderBy(p =>
                    db.Skus.Where(s => s.ProductId == p.Id)
                           .Min(s => (decimal?)s.Price) ?? 0),
                "price_desc" => query.OrderByDescending(p =>
                    db.Skus.Where(s => s.ProductId == p.Id)
                           .Min(s => (decimal?)s.Price) ?? 0),
                "date_desc" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var total = await query.CountAsync(ct);

            var items = await query
                .Skip(offset)
                .Take(limit)
                .Include(p => p.Images.OrderBy(i => i.Order))
                .Include(p => p.Skus)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<(List<Product> Items, int TotalCount)> GetSimilarAsync(
            Guid productId,
            Guid categoryId,
            int limit,
            int offset,
            CancellationToken ct = default)
        {
            var query = db.Products
                .AsNoTracking()
                .Where(p =>
                    p.Status == ProductStatus.Moderated &&
                    p.Id != productId &&
                    p.CategoryId == categoryId)
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip(offset)
                .Take(limit)
                .Include(p => p.Images.OrderBy(i => i.Order))
                .Include(p => p.Skus)
                .ToListAsync(ct);

            return (items, total);
        }
    }

}
