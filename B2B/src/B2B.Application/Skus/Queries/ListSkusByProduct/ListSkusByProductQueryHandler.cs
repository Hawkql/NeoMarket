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

namespace B2B.Application.Skus.Queries.ListSkusByProduct
{

    public sealed class ListSkusByProductQueryHandler
        : IRequestHandler<ListSkusByProductQuery, IReadOnlyList<SkuResponseDto>>
    {
        private readonly ISkuRepository _skuRepository;
        private readonly IProductRepository _productRepository;
        private readonly IImageRepository _imageRepository;

        public ListSkusByProductQueryHandler(
            ISkuRepository skuRepository,
            IProductRepository productRepository,
            IImageRepository imageRepository)
        {
            _skuRepository = skuRepository;
            _productRepository = productRepository;
            _imageRepository = imageRepository;
        }

        public async Task<IReadOnlyList<SkuResponseDto>> Handle(
            ListSkusByProductQuery request,
            CancellationToken ct)
        {
            // Ownership: товар существует и принадлежит продавцу
            var product = await _productRepository.GetByIdAsync(request.ProductId, ct);
            if (product is null || product.Deleted || product.SellerId != request.SellerId)
                throw new DomainException("Product not found", "NOT_FOUND");

            var skus = await _skuRepository.GetByProductIdAsync(request.ProductId, ct);

            if (skus.Count == 0)
                return Array.Empty<SkuResponseDto>();

            // Картинки всех SKU одним batch-запросом (против N+1)
            var skuIds = skus.Select(s => s.Id).ToList();
            var imagesMap = await _imageRepository.GetByEntitiesAsync(
                ImageEntityType.Sku, skuIds, ct);

            return skus
                .Select(s =>
                {
                    var imgs = imagesMap.TryGetValue(s.Id, out var list)
                        ? list
                        : Array.Empty<Image>();
                    return SkuDtoMapper.Map(s, imgs);
                })
                .ToList();
        }
    }
}
