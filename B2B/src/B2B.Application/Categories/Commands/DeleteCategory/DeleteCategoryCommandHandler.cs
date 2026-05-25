using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Categories;
using B2B.Domain.Common;
using MediatR;

namespace B2B.Application.Categories.Commands.DeleteCategory
{

    public sealed class DeleteCategoryCommandHandler
        : IRequestHandler<DeleteCategoryCommand>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteCategoryCommand request, CancellationToken ct)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct);
            if (category is null || category.Deleted)
                throw new DomainException("Category not found", "NOT_FOUND");

            // 409 если в категории есть товары (спека: удалять можно только пустую)
            if (await _categoryRepository.HasProductsAsync(category.Id, ct))
                throw new DomainException(
                    "Cannot delete category with products", "CONFLICT");

            // Также нельзя удалять, если есть активные подкатегории — иначе осиротеют.
            // Проверяем прямых детей.
            var children = await _categoryRepository.GetChildrenAsync(category.Id, ct);
            if (children.Count > 0)
                throw new DomainException(
                    "Cannot delete category with subcategories", "CONFLICT");

            // Soft-delete (MarkAsDeleted). Физически не удаляем — категория может быть
            // в истории товаров. Спека возвращает 204.
            category.MarkAsDeleted();
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
