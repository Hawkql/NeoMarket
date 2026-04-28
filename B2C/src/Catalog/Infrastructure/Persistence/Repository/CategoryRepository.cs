using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repository;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;
namespace Infrastructure.Persistence.Repository
{
    public sealed class CategoryRepository(CatalogDbContext db) : ICategoryRepository
    {
        public async Task<List<Category>> GetAllFlatAsync(CancellationToken ct = default)
        {
            return await db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await db.Categories
                .AsNoTracking()
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<int> CountProductsAsync(Guid categoryId, CancellationToken ct = default)
        {
            return await db.Products
                .AsNoTracking()
                .CountAsync(p => p.CategoryId == categoryId && p.Status == ProductStatus.Moderated, ct);
        }

        /// <summary>
        /// Цепочка предков через рекурсивный CTE PostgreSQL.
        /// Возвращает путь ОТ КОРНЯ до указанного узла (ORDER BY level DESC).
        /// </summary>
        public async Task<List<Category>> GetAncestorsAsync(Guid categoryId, CancellationToken ct = default)
        {
            var sql = $"""
            WITH RECURSIVE ancestors AS (
                SELECT id, name, slug, parent_id, image_url, is_active,
                       seo_title, seo_description, seo_keywords_json,
                       og_title, og_description, og_image, twitter_card,
                       description, sort_order, created_at, updated_at,
                       0 AS level
                FROM catalog.categories
                WHERE id = '{categoryId}'
 
                UNION ALL
 
                SELECT c.id, c.name, c.slug, c.parent_id, c.image_url, c.is_active,
                       c.seo_title, c.seo_description, c.seo_keywords_json,
                       c.og_title, c.og_description, c.og_image, c.twitter_card,
                       c.description, c.sort_order, c.created_at, c.updated_at,
                       a.level + 1
                FROM catalog.categories c
                INNER JOIN ancestors a ON c.id = a.parent_id
            )
            SELECT * FROM ancestors ORDER BY level DESC
            """;

            return await db.Categories
                .FromSqlRaw(sql)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<Guid?> GetCategoryIdByProductAsync(Guid productId, CancellationToken ct = default)
        {
            return await db.Products
                .AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => p.CategoryId)
                .FirstOrDefaultAsync(ct);
        }
    }
}
