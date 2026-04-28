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
    public sealed class CategoryFilterRepository(CatalogDbContext db) : ICategoryFilterRepository
    {
        public async Task<List<CategoryFilter>> GetByCategoryIdAsync(
            Guid categoryId, CancellationToken ct = default)
        {
            return await db.CategoryFilters
                .AsNoTracking()
                .Where(f => f.CategoryId == categoryId)
                .ToListAsync(ct);
        }

        /// <summary>
        /// Фасеты — подсчёт товаров по каждому значению характеристики в категории.
        /// Применяем активные фильтры, чтобы счётчики обновлялись при изменении выбора.
        /// </summary>
        public async Task<List<(string FilterName, string Value, int Count)>> GetFacetsAsync(
            Guid categoryId,
            Dictionary<string, string>? activeFilters,
            CancellationToken ct = default)
        {
            var query = db.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == categoryId && p.Status == ProductStatus.Moderated);

            if (activeFilters is { Count: > 0 })
            {
                foreach (var (key, value) in activeFilters)
                {
                    var k = key.ToLower();
                    var v = value.ToLower();
                    query = query.Where(p =>
                        p.Characteristics.Any(c =>
                            c.Name.ToLower() == k && c.Value.ToLower() == v));
                }
            }

            var result = await query
                .SelectMany(p => p.Characteristics)
                .GroupBy(c => new { c.Name, c.Value })
                .Select(g => new { g.Key.Name, g.Key.Value, Count = g.Count() })
                .ToListAsync(ct);

            return result.Select(r => (r.Name, r.Value, r.Count)).ToList();
        }
    }
}
