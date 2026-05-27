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

namespace B2B.Application.Skus.Commands.UpdateSku
{
    public sealed class UpdateSkuCommandHandler
    : IRequestHandler<UpdateSkuCommand, SkuResponseDto>
    {
        private readonly ISkuRepository _skuRepository;
        private readonly IProductRepository _productRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSkuCommandHandler(
            ISkuRepository skuRepository,
            IProductRepository productRepository,
            IImageRepository imageRepository,
            IUnitOfWork unitOfWork)
        {
            _skuRepository = skuRepository;
            _productRepository = productRepository;
            _imageRepository = imageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SkuResponseDto> Handle(
            UpdateSkuCommand request,
            CancellationToken ct)
        {
            var sku = await _skuRepository.GetByIdAsync(request.SkuId, ct);

            if (sku is null || sku.Deleted)
                throw new DomainException("SKU not found", "NOT_FOUND");

            // Ownership через товар
            var product = await _productRepository.GetByIdAsync(sku.ProductId, ct);
            if (product is null)
                throw new DomainException("SKU not found", "NOT_FOUND");
            if (product.SellerId != request.SellerId)
                throw new DomainException(
                    "Product does not belong to the authenticated seller", "NOT_OWNER");

            // HARD_BLOCKED товар → редактировать SKU нельзя (403)
            product.EnsureCanBeEdited();

            // PATCH-merge: неизменённые поля — из текущего состояния SKU.
            // ВАЖНО: reserved_quantity и active_quantity НЕ трогаются (US-03:
            // reserves_preserved_after_sku_edit). Sku.Update их не меняет в принципе.
            var newName = request.Name ?? sku.Name;
            var newPrice = request.Price ?? sku.Price;
            var newDiscount = request.Discount ?? sku.Discount;
            var newCostPrice = request.CostPrice ?? sku.CostPrice;
            var newArticle = request.Article ?? sku.Article;

            IEnumerable<SkuCharacteristic>? characteristics =
                request.Characteristics?
                    .Select(c => new SkuCharacteristic(c.Name, c.Value))
                    .ToList();

            sku.Update(
                name: newName,
                price: newPrice,
                costPrice: newCostPrice,
                discount: newDiscount,
                article: newArticle,
                characteristics: characteristics);

            // Правка SKU → товар на повторную модерацию (если MODERATED/BLOCKED).
            // Контент товара не меняется, только статус + событие EDITED.
            product.SendToModerationOnEdit();

            await _unitOfWork.SaveChangesAsync(ct);

            var images = await _imageRepository.GetByEntityAsync(
                ImageEntityType.Sku, sku.Id, ct);

            return SkuDtoMapper.Map(sku, images);
        }
    }
}
