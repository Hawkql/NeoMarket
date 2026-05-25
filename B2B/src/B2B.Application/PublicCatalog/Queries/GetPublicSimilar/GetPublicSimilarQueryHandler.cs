using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.PublicCatalog.Dtos;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.PublicCatalog.Queries.GetPublicSimilar
{
    public sealed class GetPublicSimilarQueryHandler
    : IRequestHandler<GetPublicSimilarQuery, IReadOnlyList<ProductPublicShortDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IImageRepository _imageRepository;

        public GetPublicSimilarQueryHandler(
            IProductRepository productRepository,
            ISkuRepository skuRepository,
            IImageRepository imageRepository)
        {
            _productRepository = productRepository;
            _skuRepository = skuRepository;
            _imageRepository = imageRepository;
        }

        public async Task<IReadOnlyList<ProductPublicShortDto>> Handle(
            GetPublicSimilarQuery request,
            CancellationToken ct)
        {
            // Берём категорию исходного товара. Если товар не витринный — пустой список
            // (similar для скрытого товара не имеет смысла, но и не ошибка).
            var product = await _productRepository.GetByIdAsync(request.ProductId, ct);
            if (product is null || product.Deleted)
                return Array.Empty<ProductPublicShortDto>();

            var similar = await _productRepository.GetSimilarAsync(
                request.ProductId, product.CategoryId, request.Limit, ct);

            if (similar.Count == 0)
                return Array.Empty<ProductPublicShortDto>();

            var ids = similar.Select(p => p.Id).ToList();

            var imagesMap = await _imageRepository.GetByEntitiesAsync(
                ImageEntityType.Product, ids, ct);
            var minPriceMap = await _skuRepository.GetMinPriceByProductIdsAsync(ids, ct);

            return similar.Select(p =>
            {
                string? cover = imagesMap.TryGetValue(p.Id, out var imgs)
                    ? imgs.OrderBy(i => i.Ordering).Select(i => i.Url).FirstOrDefault()
                    : null;
                int? minPrice = minPriceMap.TryGetValue(p.Id, out var mp) ? mp : null;

                return new ProductPublicShortDto(
                    Id: p.Id,
                    Title: p.Title,
                    Slug: p.Slug,
                    Status: p.Status,
                    CategoryId: p.CategoryId,
                    MinPrice: minPrice,
                    CoverImage: cover,
                    CreatedAt: p.CreatedAt);
            }).ToList();
        }
    }
}
