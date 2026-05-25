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

namespace B2B.Application.PublicCatalog.Queries.BatchPublicProducts
{
    public sealed class BatchPublicProductsQueryHandler
    : IRequestHandler<BatchPublicProductsQuery, IReadOnlyList<ProductPublicDto>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IImageRepository _imageRepository;

        public BatchPublicProductsQueryHandler(
            IProductRepository productRepository,
            ISkuRepository skuRepository,
            IImageRepository imageRepository)
        {
            _productRepository = productRepository;
            _skuRepository = skuRepository;
            _imageRepository = imageRepository;
        }

        public async Task<IReadOnlyList<ProductPublicDto>> Handle(
            BatchPublicProductsQuery request,
            CancellationToken ct)
        {
            if (request.ProductIds.Count == 0)
                return Array.Empty<ProductPublicDto>();

            // Витринные товары (Moderated, не deleted). Несуществующие просто отсутствуют.
            var products = await _productRepository.GetPublicByIdsAsync(request.ProductIds, ct);
            if (products.Count == 0)
                return Array.Empty<ProductPublicDto>();

            var productIds = products.Select(p => p.Id).ToList();

            // SKU всех товаров — один batch
            var allSkus = await _skuRepository.GetByProductIdsAsync(productIds, ct);
            var skusByProduct = allSkus
                .GroupBy(s => s.ProductId)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Sku>)g.ToList());

            // Картинки товаров — batch
            var productImagesMap = await _imageRepository.GetByEntitiesAsync(
                ImageEntityType.Product, productIds, ct);

            // Картинки всех SKU — batch
            var skuIds = allSkus.Select(s => s.Id).ToList();
            var skuImagesMap = skuIds.Count > 0
                ? await _imageRepository.GetByEntitiesAsync(ImageEntityType.Sku, skuIds, ct)
                : new Dictionary<Guid, IReadOnlyCollection<Image>>();

            // Собираем карточки. Витринное правило: показываем товар, даже если все
            // SKU out-of-stock — B2C сам решит по active_quantity (корзина показывает
            // "нет в наличии"). Товар без единого SKU тоже валиден для карточки.
            var result = new List<ProductPublicDto>(products.Count);
            foreach (var product in products)
            {
                var liveSkus = skusByProduct.TryGetValue(product.Id, out var s)
                    ? s
                    : Array.Empty<Sku>();

                var productImages = productImagesMap.TryGetValue(product.Id, out var pi)
                    ? pi
                    : Array.Empty<Image>();

                result.Add(PublicCatalogMapper.MapProduct(
                    product, productImages, liveSkus, skuImagesMap));
            }

            return result;
        }
    }
}
