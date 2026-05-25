using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Categories.Dtos;
using B2B.Domain.Categories;
using MediatR;

namespace B2B.Application.Categories.Queries.GetCategoriesTree
{
    public sealed class GetCategoriesTreeQueryHandler
    : IRequestHandler<GetCategoriesTreeQuery, IReadOnlyList<CategoryTreeDto>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetCategoriesTreeQueryHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IReadOnlyList<CategoryTreeDto>> Handle(
            GetCategoriesTreeQuery request,
            CancellationToken ct)
        {
            // Все активные категории одним запросом
            var all = await _categoryRepository.GetAllAsync(includeDeleted: false, ct);

            // Группируем по ParentId для O(1) доступа к детям
            var byParent = all
                .GroupBy(c => c.ParentId)
                .ToDictionary(g => g.Key ?? Guid.Empty, g => g.ToList());

            // Рекурсивно строим дерево от корней (ParentId == null)
            return BuildTree(null, byParent);
        }

        private static IReadOnlyList<CategoryTreeDto> BuildTree(
            Guid? parentId,
            IReadOnlyDictionary<Guid, List<Category>> byParent)
        {
            var key = parentId ?? Guid.Empty;
            if (!byParent.TryGetValue(key, out var children))
                return Array.Empty<CategoryTreeDto>();

            return children
                .OrderBy(c => c.Ordering)
                .Select(c => new CategoryTreeDto(
                    Id: c.Id,
                    Name: c.Name,
                    Children: BuildTree(c.Id, byParent)))
                .ToList();
        }
    }
}
