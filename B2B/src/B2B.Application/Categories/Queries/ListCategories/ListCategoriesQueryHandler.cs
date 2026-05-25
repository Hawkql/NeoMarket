using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Categories.Dtos;
using B2B.Domain.Categories;
using MediatR;

namespace B2B.Application.Categories.Queries.ListCategories
{
    public sealed class ListCategoriesQueryHandler
    : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public ListCategoriesQueryHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IReadOnlyList<CategoryDto>> Handle(
            ListCategoriesQuery request,
            CancellationToken ct)
        {
            // only_root → корневые (parent_id IS NULL); иначе дети parent_id
            // (если parent_id не задан и not only_root → дети null = тоже корневые)
            IReadOnlyCollection<Category> categories;

            if (request.OnlyRoot)
                categories = await _categoryRepository.GetChildrenAsync(null, ct);
            else
                categories = await _categoryRepository.GetChildrenAsync(request.ParentId, ct);

            if (categories.Count == 0)
                return Array.Empty<CategoryDto>();

            var ids = categories.Select(c => c.Id).ToList();
            var levelPath = await _categoryRepository.GetLevelAndPathAsync(ids, ct);

            return categories
                .Select(c => CategoryDtoMapper.Map(c, levelPath))
                .ToList();
        }
    }
}
