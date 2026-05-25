using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Categories.Dtos;
using B2B.Domain.Categories;
using B2B.Domain.Common;
using MediatR;

namespace B2B.Application.Categories.Queries.GetBreadcrumbs
{
    public sealed class GetBreadcrumbsQueryHandler
    : IRequestHandler<GetBreadcrumbsQuery, IReadOnlyList<CategoryDto>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetBreadcrumbsQueryHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IReadOnlyList<CategoryDto>> Handle(
            GetBreadcrumbsQuery request,
            CancellationToken ct)
        {
            // Цепочка корень → текущая
            var chain = await _categoryRepository.GetAncestorsChainAsync(request.CategoryId, ct);
            if (chain.Count == 0)
                throw new DomainException("Category not found", "NOT_FOUND");

            var ids = chain.Select(c => c.Id).ToList();
            var levelPath = await _categoryRepository.GetLevelAndPathAsync(ids, ct);

            return chain
                .Select(c => CategoryDtoMapper.Map(c, levelPath))
                .ToList();
        }
    }
}
