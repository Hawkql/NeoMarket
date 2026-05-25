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
        public async Task<IReadOnlyDictionary<Guid, (int Level, string Path)>> GetLevelAndPathAsync(
    IEnumerable<Guid> categoryIds, CancellationToken ct)
        {
            var ids = categoryIds.Distinct().ToArray();
            if (ids.Length == 0)
                return new Dictionary<Guid, (int, string)>();

            // Восходящий CTE: для каждой целевой категории поднимаемся к корню,
            // считаем глубину и собираем path из имён (нижний регистр, через '/').
            // depth: у корня 0; path строим от корня вниз.
            var rows = await _dbContext.Database
                .SqlQuery<CategoryPathRow>($"""
            WITH RECURSIVE chain AS (
                SELECT id AS target_id, id, parent_id, name, 0 AS up_depth
                FROM categories
                WHERE id = ANY({ids})
                UNION ALL
                SELECT c.target_id, p.id, p.parent_id, p.name, c.up_depth + 1
                FROM categories p
                INNER JOIN chain c ON c.parent_id = p.id
            )
            SELECT
                target_id                                   AS "TargetId",
                (MAX(up_depth))                             AS "Level",
                string_agg(lower(name), '/' ORDER BY up_depth DESC) AS "Path"
            FROM chain
            GROUP BY target_id
            """)
                .ToListAsync(ct);

            return rows.ToDictionary(
                r => r.TargetId,
                r => (r.Level, r.Path ?? string.Empty));
        }

        public async Task<IReadOnlyList<Category>> GetAncestorsChainAsync(
            Guid categoryId, CancellationToken ct)
        {
            // Поднимаемся к корню, затем переворачиваем (корень → текущая)
            var chain = await _dbContext.Categories
           .FromSql($"""
                WITH RECURSIVE ancestors AS (
                    SELECT * FROM categories WHERE id = {categoryId}
                    UNION ALL
                    SELECT c.* FROM categories c
                    INNER JOIN ancestors a ON a.parent_id = c.id
                )
                SELECT * FROM ancestors
                """)
           .AsNoTracking()
       .ToListAsync(ct);

            // CTE отдаёт от текущей вверх; для breadcrumbs нужен порядок корень→текущая.
            // Сортируем по «расстоянию до корня»: чем меньше предков выше — тем ближе к корню.
            // Проще — построить порядок в памяти по ParentId-цепочке.
            return OrderRootToLeaf(chain, categoryId);
        }

        public async Task<bool> HasProductsAsync(Guid categoryId, CancellationToken ct)
        {
            return await _dbContext.Products
                .AsNoTracking()
                .AnyAsync(p => p.CategoryId == categoryId && !p.Deleted, ct);
        }

        // Вспомогательное: упорядочить цепочку от корня к листу
        private static IReadOnlyList<Category> OrderRootToLeaf(
            List<Category> chain, Guid leafId)
        {
            var byId = chain.ToDictionary(c => c.Id);
            var ordered = new LinkedList<Category>();
            var currentId = (Guid?)leafId;

            while (currentId is not null && byId.TryGetValue(currentId.Value, out var node))
            {
                ordered.AddFirst(node);     // вставляем в начало → корень окажется первым
                currentId = node.ParentId;
            }

            return ordered.ToList();
        }
    }
}
