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
using MediatR;

namespace B2B.Application.Products.Commands.CreateProduct
{
    public sealed class CreateProductCommandHandler
    : IRequestHandler<CreateProductCommand, ProductDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IImageRepository _imageRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateProductCommandHandler(
            IProductRepository productRepository,
            IImageRepository imageRepository,
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _imageRepository = imageRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProductDto> Handle(
            CreateProductCommand request,
            CancellationToken ct)
        {
            // 1. Проверка существования категории (бизнес-правило из спеки: "Category not found" → 400)
            var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId, ct);
            if (!categoryExists)
                throw new DomainException("Category not found", "INVALID_REQUEST");

            // 2. Создание агрегата Product через фабрику (вся валидация инвариантов внутри)
            var characteristics = request.Characteristics
                .Select(c => new ProductCharacteristic(c.Name, c.Value));

            var product = Product.Create(
                request.SellerId,
                request.CategoryId,
                request.Title,
                request.Description,
                characteristics);

            await _productRepository.AddAsync(product, ct);

            Console.WriteLine($">>> PRODUCT ID: {product.Id}");
            // 3. Создание Image-агрегатов (polymorphic, entity_type = product)
            var images = new List<Image>();
            foreach (var img in request.Images)
            {
                var image = Image.Create(
                    ImageEntityType.Product,
                    product.Id,
                    img.Url,
                    img.Ordering);
                await _imageRepository.AddAsync(image, ct);
                images.Add(image);
            }

            // 4. Сохранение — Product + Images + Outbox в одной транзакции
            await _unitOfWork.SaveChangesAsync(ct);

            // 5. Маппинг в DTO для ответа
            return new ProductDto(
                Id: product.Id,
                Title: product.Title,
                Description: product.Description,
                Status: product.Status.ToString().ToUpperInvariant(),  // "CREATED"
                Deleted: product.Deleted,
                Blocked: product.Blocked,
                CategoryId: product.CategoryId,
                Images: images
                    .OrderBy(i => i.Ordering)
                    .Select(i => new ImageDto(i.Id, i.Url, i.Ordering))
                    .ToList(),
                Characteristics: product.Characteristics
                    .Select(c => new CharacteristicDto(c.Name, c.Value))
                    .ToList(),
                Skus: new List<SkuDto>(),  // при создании SKU всегда пусто
                CreatedAt: product.CreatedAt,
                UpdatedAt: product.UpdatedAt);
        }
    }
}
