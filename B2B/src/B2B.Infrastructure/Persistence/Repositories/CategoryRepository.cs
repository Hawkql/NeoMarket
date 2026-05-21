using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence.Repositories
{
    public sealed class CategoryRepository : ICategoryRepository
    {
        private readonly B2BDbContext _dbContext;

        public CategoryRepository(B2BDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _dbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        {
            // AnyAsync генерирует SELECT EXISTS(...) — быстрее, чем COUNT
            return await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Id == id && !c.Deleted, ct);
        }

        public async Task<IReadOnlyCollection<Category>> GetAllAsync(
            bool includeDeleted,
            CancellationToken ct)
        {
            var query = _dbContext.Categories.AsNoTracking();

            if (!includeDeleted)
                query = query.Where(c => !c.Deleted);

            return await query
                .OrderBy(c => c.ParentId)
                .ThenBy(c => c.Ordering)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyCollection<Category>> GetChildrenAsync(
            Guid? parentId,
            CancellationToken ct)
        {
            // Использует ix_categories_parent_active (partial index по deleted = false)
            return await _dbContext.Categories
                .AsNoTracking()
                .Where(c => c.ParentId == parentId && !c.Deleted)
                .OrderBy(c => c.Ordering)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyCollection<Guid>> GetDescendantIdsAsync(
            Guid categoryId,
            CancellationToken ct)
        {
            

            // FromSql возвращает IQueryable<Category>, но нам нужны только id.
            // Используем Database.SqlQuery<Guid> — он умеет проектировать в скаляр.
            var ids = await _dbContext.Database
                .SqlQuery<Guid>($"""
                WITH RECURSIVE descendants AS (
                    SELECT id FROM categories WHERE id = {categoryId}
                    UNION ALL
                    SELECT c.id 
                    FROM categories c
                    INNER JOIN descendants d ON c.parent_id = d.id
                    WHERE c.deleted = false
                )
                SELECT id FROM descendants
                """)
                .ToListAsync(ct);

            return ids;
        }

        public async Task AddAsync(Category category, CancellationToken ct)
        {
            await _dbContext.Categories.AddAsync(category, ct);
        }

        public void Remove(Category category)
        {
            _dbContext.Categories.Remove(category);
        }
    }
}
