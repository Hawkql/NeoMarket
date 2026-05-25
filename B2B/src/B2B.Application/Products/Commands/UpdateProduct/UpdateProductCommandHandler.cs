using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Application.Products.Dtos;
using B2B.Domain.Categories;
using B2B.Domain.Common;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Products.Commands.UpdateProduct
{
    public sealed class UpdateProductCommandHandler
    : IRequestHandler<UpdateProductCommand, ProductDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateProductCommandHandler(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ISkuRepository skuRepository,
            IImageRepository imageRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _skuRepository = skuRepository;
            _imageRepository = imageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProductDto> Handle(
            UpdateProductCommand request,
            CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId, ct);

            if (product is null || product.Deleted)
                throw new DomainException("Product not found", "NOT_FOUND");

            // IDOR: чужой товар → 404 (не раскрываем существование)
            if (product.SellerId != request.SellerId)
                throw new DomainException("Product not found", "NOT_FOUND");

            // PATCH-merge: неизменённые поля берём из текущего состояния
            var newTitle = request.Title ?? product.Title;
            var newDescription = request.Description ?? product.Description;
            var newCategoryId = request.CategoryId ?? product.CategoryId;

            // Если меняется категория — она должна существовать
            if (request.CategoryId is not null &&
                request.CategoryId.Value != product.CategoryId)
            {
                var exists = await _categoryRepository.ExistsAsync(
                    request.CategoryId.Value, ct);
                if (!exists)
                    throw new DomainException("Category not found", "INVALID_REQUEST");
            }

            // null = не трогать характеристики; иначе — заменить целиком
            IEnumerable<ProductCharacteristic>? characteristics =
                request.Characteristics?
                    .Select(c => new ProductCharacteristic(c.Name, c.Value))
                    .ToList();

            // Доменный метод сам проверит HARD_BLOCKED/deleted (EnsureCanBeEdited),
            // валидацию контента и при MODERATED/BLOCKED уведёт на повторную модерацию.
            product.Update(newCategoryId, newTitle, newDescription, characteristics);

            await _unitOfWork.SaveChangesAsync(ct);

            // Полный ProductResponse (с актуальными images + skus)
            var productImages = await _imageRepository.GetByEntityAsync(
                ImageEntityType.Product, product.Id, ct);
            var skus = await _skuRepository.GetByProductIdAsync(product.Id, ct);

            return ProductDtoMapper.Map(product, productImages, skus);
        }
    }
}
