using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Categories
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(Guid id,CancellationToken ct);
        Task<bool> ExistsAsync(Guid id,CancellationToken ct);
        Task<IReadOnlyCollection<Category>> GetAllAsync(
            bool includeDeleted,
            CancellationToken ct);


        /// <summary>
        /// Прямые дети категории.
        /// </summary>
        Task<IReadOnlyCollection<Category>> GetChildrenAsync(
            Guid? parentId,  // null = корневые
            CancellationToken ct);

        /// <summary>
        /// Все потомки категории (включая её саму) — рекурсивно через SQL.
        /// Используется для проверки циклов в HierarchyValidator
        /// и для запроса "товары в категории и подкатегориях".
        /// </summary>
        Task<IReadOnlyCollection<Guid>> GetDescendantIdsAsync(
            Guid categoryId,
            CancellationToken ct);

        Task AddAsync(Category category, CancellationToken ct);
        void Remove(Category category);
        /// <summary>
        /// level (глубина от корня, корень=0) и path (slug-цепочка имён) для категорий.
        /// Вычисляется восходящим CTE. Ключ — category_id.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, (int Level, string Path)>> GetLevelAndPathAsync(
            IEnumerable<Guid> categoryIds, CancellationToken ct);

        /// <summary>Цепочка от корня до категории включительно (для breadcrumbs).</summary>
        Task<IReadOnlyList<Category>> GetAncestorsChainAsync(
            Guid categoryId, CancellationToken ct);

        /// <summary>Есть ли у категории привязанные (не удалённые) товары — для delete.</summary>
        Task<bool> HasProductsAsync(Guid categoryId, CancellationToken ct);
    }
}
