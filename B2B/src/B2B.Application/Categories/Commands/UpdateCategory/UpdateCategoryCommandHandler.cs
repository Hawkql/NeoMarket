using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Categories.Dtos;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Categories;
using B2B.Domain.Common;
using MediatR;

namespace B2B.Application.Categories.Commands.UpdateCategory
{

    public sealed class UpdateCategoryCommandHandler
        : IRequestHandler<UpdateCategoryCommand, CategoryWithChildrenDto>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICategoryHierarchyValidator _hierarchyValidator;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            ICategoryHierarchyValidator hierarchyValidator,
            IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _hierarchyValidator = hierarchyValidator;
            _unitOfWork = unitOfWork;
        }

        public async Task<CategoryWithChildrenDto> Handle(
            UpdateCategoryCommand request,
            CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct);
            if (category is null)
                throw new DomainException("Category not found", "NOT_FOUND");

            // 1. Переименование
            if (request.Name is not null)
                category.Rename(request.Name);

            // 2. Перемещение (только если parent_id физически присутствовал в запросе)
            if (request.ParentIdSpecified && request.ParentId != category.ParentId)
            {
                if (request.ParentId is not null)
                {
                    // Новый родитель должен существовать
                    await _hierarchyValidator.EnsureParentExistsAsync(request.ParentId.Value, ct);
                    // И перемещение не должно создать цикл
                    await _hierarchyValidator.EnsureNoCycleAsync(
                        category.Id, request.ParentId.Value, ct);
                }
                category.MoveTo(request.ParentId);
            }

            // 3. Активация / деактивация
            if (request.IsActive is not null)
            {
                if (request.IsActive.Value && category.Deleted)
                    category.Restore();
                else if (!request.IsActive.Value && !category.Deleted)
                    category.MarkAsDeleted();
            }

            await _unitOfWork.SaveChangesAsync(ct);

            // Ответ с детьми
            var children = await _categoryRepository.GetChildrenAsync(category.Id, ct);
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
                Children: children.Select(c => CategoryDtoMapper.Map(c, levelPath)).ToList());
        }
    }
}
