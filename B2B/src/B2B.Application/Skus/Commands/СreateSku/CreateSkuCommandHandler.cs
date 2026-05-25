using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Application.Skus.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Skus.Commands.СreateSku
{
    public sealed class CreateSkuCommandHandler
    : IRequestHandler<CreateSkuCommand, SkuResponseDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateSkuCommandHandler(
            IProductRepository productRepository,
            ISkuRepository skuRepository,
            IImageRepository imageRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _skuRepository = skuRepository;
            _imageRepository = imageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SkuResponseDto> Handle(
            CreateSkuCommand request,
            CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId, ct);

            if (product is null || product.Deleted)
                throw new DomainException("Product not found", "NOT_FOUND");

            // IDOR: SKU можно добавлять только в свой товар → чужой = 404
            if (product.SellerId != request.SellerId)
                throw new DomainException("Product not found", "NOT_FOUND");

            // Проверка статуса (HARD_BLOCKED/deleted → 403). Бросает DomainException FORBIDDEN.
            product.EnsureCanAddSku();

            // Cover — первая картинка по ordering (или пусто, если картинок нет)
            var coverUrl = request.Images
                .OrderBy(i => i.Ordering)
                .Select(i => i.Url)
                .FirstOrDefault() ?? string.Empty;

            var characteristics = request.Characteristics
                .Select(c => new SkuCharacteristic(c.Name, c.Value))
                .ToList();

            var sku = Sku.Create(
                productId: request.ProductId,
                name: request.Name,
                price: request.Price,
                costPrice: request.CostPrice,
                discount: request.Discount,
                article: request.Article,
                imageUrl: coverUrl,
                characteristics: characteristics);

            await _skuRepository.AddAsync(sku, ct);

            // Галерея SKU — в polymorphic images (entity_type=sku)
            var skuImages = new List<Image>();
            foreach (var img in request.Images)
            {
                var image = Image.Create(ImageEntityType.Sku, sku.Id, img.Url, img.Ordering);
                await _imageRepository.AddAsync(image, ct);
                skuImages.Add(image);
            }

            // Первый ли это SKU товара? Если да — товар уходит на модерацию.
            // CountByProductIdAsync считает уже существующие (без только что добавленного,
            // т.к. SaveChanges ещё не вызван — в БД нового SKU нет).
            var existingSkuCount = await _skuRepository.CountByProductIdAsync(
                request.ProductId, ct);

            if (existingSkuCount == 0)
            {
                // CREATED → ON_MODERATION + ProductSentToModerationEvent (идёт в Moderation)
                product.SendToModerationOnFirstSku();
            }

            // Product + Sku + Images + Outbox — одна транзакция
            await _unitOfWork.SaveChangesAsync(ct);

            return SkuDtoMapper.Map(sku, skuImages);
        }
    }
}
