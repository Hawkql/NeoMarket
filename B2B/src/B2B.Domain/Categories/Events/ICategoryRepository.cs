using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Categories.Events
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
    }
}
