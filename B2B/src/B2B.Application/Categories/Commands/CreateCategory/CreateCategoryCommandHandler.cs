using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Categories.Dtos;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Categories;
using MediatR;

namespace B2B.Application.Categories.Commands.CreateCategory
{
    public sealed class CreateCategoryCommandHandler
    : IRequestHandler<CreateCategoryCommand, CategoryWithChildrenDto>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICategoryHierarchyValidator _hierarchyValidator;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            ICategoryHierarchyValidator hierarchyValidator,
            IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _hierarchyValidator = hierarchyValidator;
            _unitOfWork = unitOfWork;
        }

        public async Task<CategoryWithChildrenDto> Handle(
            CreateCategoryCommand request,
            CancellationToken ct)
        {
            // Если задан родитель — он должен существовать и быть активным
            if (request.ParentId is not null)
                await _hierarchyValidator.EnsureParentExistsAsync(request.ParentId.Value, ct);

            // ordering спекой при создании не задаётся → 0 по умолчанию
            var category = Category.Create(request.ParentId, request.Name, ordering: 0);

            await _categoryRepository.AddAsync(category, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // level/path для только что созданной (детей пока нет)
            var levelPath = await _categoryRepository.GetLevelAndPathAsync(
                new[] { category.Id }, ct);
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
                Children: Array.Empty<CategoryDto>());
        }
    }
}
