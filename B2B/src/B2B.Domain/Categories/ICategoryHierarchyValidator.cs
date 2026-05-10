using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Categories
{
    public interface ICategoryHierarchyValidator
    {
        /// <summary>
        /// Проверяет, что перемещение category в newParent не создаст цикл
        /// (newParent не должен быть потомком category).
        /// 
        /// Бросает DomainException с кодом CYCLE_DETECTED если цикл.
        /// </summary>
        /// 
        Task EnsureNoCycleAsync(
        Guid categoryId,
        Guid newParentId,
        CancellationToken ct);

        /// <summary>
        /// Проверяет, что parent существует и не удалена.
        /// </summary>
        Task EnsureParentExistsAsync(Guid parentId, CancellationToken ct);
    }
}
