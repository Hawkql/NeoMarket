using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Pagination;
using B2B.Application.PublicCatalog.Dtos;
using B2B.Domain.Categories;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.PublicCatalog.Queries.ListPublicProducts
{
    public sealed class ListPublicProductsQueryHandler
    : IRequestHandler<ListPublicProductsQuery, PagedResult<ProductPublicShortDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IImageRepository _imageRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ListPublicProductsQueryHandler(
            IProductRepository productRepository,
            ISkuRepository skuRepository,
            IImageRepository imageRepository,
            ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _skuRepository = skuRepository;
            _imageRepository = imageRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<PagedResult<ProductPublicShortDto>> Handle(
            ListPublicProductsQuery request,
            CancellationToken ct)
        {
            // Если задана категория — включаем её потомков (витрина показывает
            // товары категории и всех подкатегорий)
            IReadOnlyCollection<Guid>? treeIds = null;
            if (request.CategoryId is not null)
                treeIds = await _categoryRepository.GetDescendantIdsAsync(
                    request.CategoryId.Value, ct);

            var filter = new PublicCatalogFilter(
                CategoryId: request.CategoryId,
                CategoryIdsInTree: treeIds,
                Search: request.Search,
                MinPrice: request.MinPrice,
                MaxPrice: request.MaxPrice,
                SellerId: request.SellerId,
                Sort: request.Sort,
                Limit: request.Limit,
                Offset: request.Offset);

            var (products, total) = await _productRepository.GetPublicCatalogAsync(filter, ct);

            var productIds = products.Select(p => p.Id).ToList();

            // cover image + min price — batch (как в seller-списке)
            var imagesMap = productIds.Count > 0
                ? await _imageRepository.GetByEntitiesAsync(ImageEntityType.Product, productIds, ct)
                : new Dictionary<Guid, IReadOnlyCollection<Image>>();

            var minPriceMap = productIds.Count > 0
                ? await _skuRepository.GetMinPriceByProductIdsAsync(productIds, ct)
                : new Dictionary<Guid, int>();

            var items = products.Select(p =>
            {
                string? cover = imagesMap.TryGetValue(p.Id, out var imgs)
                    ? imgs.OrderBy(i => i.Ordering).Select(i => i.Url).FirstOrDefault()
                    : null;

                int minPrice = minPriceMap.TryGetValue(p.Id, out var mp) ? mp : 0;

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

            return new PagedResult<ProductPublicShortDto>(items, total, request.Limit, request.Offset);
        }
    }
}
