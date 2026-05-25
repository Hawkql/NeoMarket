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

namespace B2B.Application.PublicCatalog.Queries.GetPublicProduct
{
    public sealed class GetPublicProductQueryHandler
    : IRequestHandler<GetPublicProductQuery, ProductPublicDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IImageRepository _imageRepository;

        public GetPublicProductQueryHandler(
            IProductRepository productRepository,
            ISkuRepository skuRepository,
            IImageRepository imageRepository)
        {
            _productRepository = productRepository;
            _skuRepository = skuRepository;
            _imageRepository = imageRepository;
        }

        public async Task<ProductPublicDto> Handle(
            GetPublicProductQuery request,
            CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId, ct);

            // Витринное правило: только Moderated и не deleted, иначе 404
            if (product is null
                || product.Deleted
                || product.Status != ProductStatus.Moderated)
                throw new DomainException("Product not found", "NOT_FOUND");

            var productImages = await _imageRepository.GetByEntityAsync(
                ImageEntityType.Product, product.Id, ct);

            var skus = await _skuRepository.GetByProductIdAsync(product.Id, ct);

            var skuIds = skus.Select(s => s.Id).ToList();
            var skuImagesMap = skuIds.Count > 0
                ? await _imageRepository.GetByEntitiesAsync(ImageEntityType.Sku, skuIds, ct)
                : new Dictionary<Guid, IReadOnlyCollection<Image>>();

            return PublicCatalogMapper.MapProduct(product, productImages, skus, skuImagesMap);
        }
    }
}
