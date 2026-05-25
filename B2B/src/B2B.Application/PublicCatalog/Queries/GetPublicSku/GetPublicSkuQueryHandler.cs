using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.PublicCatalog.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.PublicCatalog.Queries.GetPublicSku
{
    public sealed class GetPublicSkuQueryHandler
    : IRequestHandler<GetPublicSkuQuery, SkuPublicDto>
    {
        private readonly ISkuRepository _skuRepository;
        private readonly IProductRepository _productRepository;
        private readonly IImageRepository _imageRepository;

        public GetPublicSkuQueryHandler(
            ISkuRepository skuRepository,
            IProductRepository productRepository,
            IImageRepository imageRepository)
        {
            _skuRepository = skuRepository;
            _productRepository = productRepository;
            _imageRepository = imageRepository;
        }

        public async Task<SkuPublicDto> Handle(
            GetPublicSkuQuery request,
            CancellationToken ct)
        {
            var sku = await _skuRepository.GetByIdAsync(request.SkuId, ct);
            if (sku is null || sku.Deleted)
                throw new DomainException("SKU not found", "NOT_FOUND");

            // Витринная видимость SKU зависит от товара: товар должен быть Moderated и не deleted
            var product = await _productRepository.GetByIdAsync(sku.ProductId, ct);
            if (product is null
                || product.Deleted
                || product.Status != ProductStatus.Moderated)
                throw new DomainException("SKU not found", "NOT_FOUND");

            var images = await _imageRepository.GetByEntityAsync(
                ImageEntityType.Sku, sku.Id, ct);

            return PublicCatalogMapper.MapSku(sku, images);
        }
    }
}
