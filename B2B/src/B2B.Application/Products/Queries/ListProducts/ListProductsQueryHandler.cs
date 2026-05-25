using B2B.Application.Common.Pagination;
using B2B.Application.Products.Dtos;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Products.Queries.ListProducts;


    public sealed class ListProductsQueryHandler
        : IRequestHandler<ListProductsQuery, PagedResult<ProductShortDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IImageRepository _imageRepository;

        public ListProductsQueryHandler(
            IProductRepository productRepository,
            ISkuRepository skuRepository,
            IImageRepository imageRepository)
        {
            _productRepository = productRepository;
            _skuRepository = skuRepository;
            _imageRepository = imageRepository;
        }

        public async Task<PagedResult<ProductShortDto>> Handle(
            ListProductsQuery request,
            CancellationToken ct)
        {
            var (products, total) = await _productRepository.GetBySellerAsync(
                request.SellerId,
                request.Status,
                request.IncludeDeleted,
                request.Limit,
                request.Offset,
                ct);

            var productIds = products.Select(p => p.Id).ToList();

            // Cover image (ordering=0) — batch
            var imagesMap = productIds.Count > 0
                ? await _imageRepository.GetByEntitiesAsync(
                    ImageEntityType.Product, productIds, ct)
                : new Dictionary<Guid, IReadOnlyCollection<Image>>();

            // MinPrice по SKU — batch
            var minPriceMap = productIds.Count > 0
                ? await _skuRepository.GetMinPriceByProductIdsAsync(productIds, ct)
                : new Dictionary<Guid, int>();

            var items = products
                .Select(p => MapToShort(p, imagesMap, minPriceMap))
                .ToList();

            return new PagedResult<ProductShortDto>(
                Items: items,
                TotalCount: total,
                Limit: request.Limit,
                Offset: request.Offset);
        }

        private static ProductShortDto MapToShort(
            Product product,
            IReadOnlyDictionary<Guid, IReadOnlyCollection<Image>> imagesMap,
            IReadOnlyDictionary<Guid, int> minPriceMap)
        {
            string? cover = null;
            if (imagesMap.TryGetValue(product.Id, out var imgs))
            {
                cover = imgs
                    .OrderBy(i => i.Ordering)
                    .Select(i => i.Url)
                    .FirstOrDefault();
            }

            int? minPrice = minPriceMap.TryGetValue(product.Id, out var mp)
                ? mp
                : null;

            return new ProductShortDto(
                Id: product.Id,
                Title: product.Title,
                Slug: product.Slug,
                Status: product.Status,
                CategoryId: product.CategoryId,
                Deleted: product.Deleted,
                CreatedAt: product.CreatedAt,
                MinPrice: minPrice,
                CoverImage: cover);
        }
    }
