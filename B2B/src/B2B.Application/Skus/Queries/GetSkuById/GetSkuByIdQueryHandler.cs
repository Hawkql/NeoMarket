using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Skus.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Skus.Queries.GetSkuById
{
    public sealed class GetSkuByIdQueryHandler
    : IRequestHandler<GetSkuByIdQuery, SkuResponseDto>
    {
        private readonly ISkuRepository _skuRepository;
        private readonly IProductRepository _productRepository;
        private readonly IImageRepository _imageRepository;

        public GetSkuByIdQueryHandler(
            ISkuRepository skuRepository,
            IProductRepository productRepository,
            IImageRepository imageRepository)
        {
            _skuRepository = skuRepository;
            _productRepository = productRepository;
            _imageRepository = imageRepository;
        }

        public async Task<SkuResponseDto> Handle(
            GetSkuByIdQuery request,
            CancellationToken ct)
        {
            var sku = await _skuRepository.GetByIdAsync(request.SkuId, ct);

            if (sku is null || sku.Deleted)
                throw new DomainException("SKU not found", "NOT_FOUND");

            // Ownership через товар: SKU → Product → seller_id из JWT
            var product = await _productRepository.GetByIdAsync(sku.ProductId, ct);
            if (product is null || product.SellerId != request.SellerId)
                throw new DomainException("SKU not found", "NOT_FOUND");

            var images = await _imageRepository.GetByEntityAsync(
                ImageEntityType.Sku, sku.Id, ct);

            return SkuDtoMapper.Map(sku, images);
        }
    }
}
