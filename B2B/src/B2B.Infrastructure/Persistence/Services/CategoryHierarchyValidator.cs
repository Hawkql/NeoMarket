using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Categories;
using B2B.Domain.Common;

namespace B2B.Infrastructure.Persistence.Services
{
    public sealed class CategoryHierarchyValidator : ICategoryHierarchyValidator
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryHierarchyValidator(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task EnsureNoCycleAsync(
            Guid categoryId,
            Guid newParentId,
            CancellationToken ct)
        {
            // Получаем все потомки (включая саму категорию).
            // Если newParent среди них — цикл.
            var descendants = await _categoryRepository.GetDescendantIdsAsync(categoryId, ct);

            if (descendants.Contains(newParentId))
                throw new DomainException(
                    $"Cannot move category {categoryId} under {newParentId}: " +
                    "this would create a cycle",
                    "CYCLE_DETECTED");
        }

        public async Task EnsureParentExistsAsync(Guid parentId, CancellationToken ct)
        {
            // ExistsAsync уже проверяет !Deleted внутри
            var exists = await _categoryRepository.ExistsAsync(parentId, ct);

            if (!exists)
                throw new DomainException(
                    $"Parent category {parentId} does not exist or is deleted",
                    "PARENT_NOT_FOUND");
        }
    }
}
