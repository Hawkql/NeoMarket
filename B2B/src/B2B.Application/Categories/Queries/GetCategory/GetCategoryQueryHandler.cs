using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Categories.Dtos;
using B2B.Domain.Categories;
using B2B.Domain.Common;
using MediatR;

namespace B2B.Application.Categories.Queries.GetCategory
{
    public sealed class GetCategoryQueryHandler
    : IRequestHandler<GetCategoryQuery, CategoryWithChildrenDto>
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetCategoryQueryHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<CategoryWithChildrenDto> Handle(
            GetCategoryQuery request,
            CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct);
            if (category is null || category.Deleted)
                throw new DomainException("Category not found", "NOT_FOUND");

            var children = await _categoryRepository.GetChildrenAsync(category.Id, ct);

            // level/path для категории и всех её детей — один запрос
            var allIds = new List<Guid> { category.Id };
            allIds.AddRange(children.Select(c => c.Id));
            var levelPath = await _categoryRepository.GetLevelAndPathAsync(allIds, ct);

            var (level, path) = levelPath.TryGetValue(category.Id, out var lp)
                ? lp
                : (0, category.Name.ToLowerInvariant());

            return new CategoryWithChildrenDto(
                Id: category.Id,
                Name: category.Name,
                ParentId: category.ParentId,
                Level: level,
                Path: path,
                IsActive: !category.Deleted,
                CreatedAt: category.CreatedAt,
                Children: children
                    .Select(c => CategoryDtoMapper.Map(c, levelPath))
                    .ToList());
        }
    }
}
