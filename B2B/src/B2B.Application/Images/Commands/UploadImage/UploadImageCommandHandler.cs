using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Application.Images.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Images.Commands.UploadImage
{
    public sealed class UploadImageCommandHandler
    : IRequestHandler<UploadImageCommand, ImageUploadResultDto>
    {
        private readonly IImageValidator _imageValidator;
        private readonly IFileStorage _fileStorage;
        private readonly IImageRepository _imageRepository;
        private readonly IProductRepository _productRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UploadImageCommandHandler(
            IImageValidator imageValidator,
            IFileStorage fileStorage,
            IImageRepository imageRepository,
            IProductRepository productRepository,
            ISkuRepository skuRepository,
            IUnitOfWork unitOfWork)
        {
            _imageValidator = imageValidator;
            _fileStorage = fileStorage;
            _imageRepository = imageRepository;
            _productRepository = productRepository;
            _skuRepository = skuRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ImageUploadResultDto> Handle(
            UploadImageCommand request,
            CancellationToken ct)
        {
            // 1. Валидация файла: magic bytes, размер ≤5МБ, формат (jpg/png/webp).
            //    Возвращает расширение или ошибку (415 UNSUPPORTED_MEDIA_TYPE / 413 FILE_TOO_LARGE).
            var validation = await _imageValidator.ValidateAsync(
                request.FileStream, request.DeclaredContentType, ct);

            if (!validation.IsValid)
                throw new DomainException(
                    validation.ErrorMessage ?? "Invalid image",
                    validation.ErrorCode ?? "INVALID_REQUEST");

            // 2. Если entity_id задан — проверяем ownership (нельзя грузить картинку
            //    к чужому товару/SKU). Неподшитое (entity_id=null) — пропускаем проверку.
            if (request.EntityId is not null)
                await EnsureOwnershipAsync(
                    request.EntityType, request.EntityId.Value, request.SellerId, ct);

            // 3. Папка в S3 по типу сущности
            var folder = request.EntityType == ImageEntityType.Product ? "products" : "skus";

            // 4. Заливаем в MinIO, получаем публичный URL
            var url = await _fileStorage.UploadAsync(
                request.FileStream, validation.Extension!, folder, ct);

            // 5. Создаём Image-агрегат (polymorphic). entity_id может быть null.
            var image = Image.Create(
                request.EntityType,
                request.EntityId ?? Guid.Empty,   // null entity_id → Guid.Empty как «неподшито»
                url,
                request.Ordering);

            await _imageRepository.AddAsync(image, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ImageUploadResultDto(
                Id: image.Id,
                Url: image.Url,
                Ordering: image.Ordering,
                EntityType: image.EntityType,
                EntityId: request.EntityId);   // отдаём как пришло (null если неподшито)
        }

        private async Task EnsureOwnershipAsync(
            ImageEntityType type, Guid entityId, Guid sellerId, CancellationToken ct)
        {
            if (type == ImageEntityType.Product)
            {
                var product = await _productRepository.GetByIdAsync(entityId, ct);
                if (product is null || product.SellerId != sellerId)
                    throw new DomainException("Product not found", "NOT_FOUND");
            }
            else // Sku
            {
                var sku = await _skuRepository.GetByIdAsync(entityId, ct);
                if (sku is null)
                    throw new DomainException("SKU not found", "NOT_FOUND");
                var product = await _productRepository.GetByIdAsync(sku.ProductId, ct);
                if (product is null || product.SellerId != sellerId)
                    throw new DomainException("SKU not found", "NOT_FOUND");
            }
        }
    }
}
