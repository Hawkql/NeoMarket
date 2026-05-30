using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;
using B2C.Application.Integration.Dtos.B2C.Application.Integration.Dtos;

namespace B2C.Tests.Infrastructure
{
    public sealed class FakeB2BCatalogClient : IB2BCatalogClient
    {
        private readonly Dictionary<Guid, SkuInfo> _skus = new();
        private readonly Dictionary<Guid, ProductSummary> _products = new();

        // ===== Test setup helpers =====

        public void SeedProduct(ProductSummary product) => _products[product.Id] = product;

        public void SeedSku(SkuInfo sku) => _skus[sku.Id] = sku;

        /// <summary>Помечает SKU как недоступный (InStock=false) — для US-CART-03 тестов.</summary>
        public void MarkOutOfStock(Guid skuId)
        {
            if (_skus.TryGetValue(skuId, out var sku))
                _skus[skuId] = sku with { InStock = false };
        }

        /// <summary>Удаляет товар (имитация PRODUCT_DELETED от B2B).</summary>
        public void RemoveProduct(Guid productId)
        {
            _products.Remove(productId);
            var toRemove = _skus.Where(kv => kv.Value.ProductId == productId)
                .Select(kv => kv.Key).ToList();
            foreach (var id in toRemove) _skus.Remove(id);
        }

        // ===== IB2BCatalogClient =====

        public Task<IReadOnlyList<SkuInfo>> GetSkusBatchAsync(IEnumerable<Guid> skuIds, CancellationToken ct)
        {
            var result = skuIds
                .Where(_skus.ContainsKey)
                .Select(id => _skus[id])
                .ToList();
            return Task.FromResult<IReadOnlyList<SkuInfo>>(result);
        }

        public Task<IReadOnlyList<ProductSummary>> GetProductsBatchAsync(IEnumerable<Guid> productIds, CancellationToken ct)
        {
            var result = productIds
                .Where(_products.ContainsKey)
                .Select(id => _products[id])
                .ToList();
            return Task.FromResult<IReadOnlyList<ProductSummary>>(result);
        }

        // ===== Остальные методы — заглушки (для текущих acceptance-тестов не нужны) =====

        public Task<CatalogPage> ListProductsAsync(CatalogQuery query, CancellationToken ct)
            => Task.FromResult(new CatalogPage(Array.Empty<ProductSummary>(), 0, query.Limit, query.Offset));

        public Task<ProductDetail?> GetProductAsync(Guid productId, CancellationToken ct)
            => Task.FromResult<ProductDetail?>(null);

        public Task<IReadOnlyList<ProductSummary>> GetSimilarAsync(Guid productId, int limit, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ProductSummary>>(Array.Empty<ProductSummary>());

        public Task<CategoryFilters> GetCategoryFiltersAsync(Guid categoryId, CancellationToken ct)
            => Task.FromResult(new CategoryFilters(Array.Empty<FilterDefinition>(), null, null));

        public Task<CategoryFilters> GetFacetsAsync(CatalogQuery query, CancellationToken ct)
            => Task.FromResult(new CategoryFilters(Array.Empty<FilterDefinition>(), null, null));

        public Task<IReadOnlyList<CategoryNode>> GetCategoryTreeAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<CategoryNode>>(Array.Empty<CategoryNode>());

        public Task<IReadOnlyList<Breadcrumb>> GetBreadcrumbsAsync(Guid? categoryId, Guid? productId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Breadcrumb>>(Array.Empty<Breadcrumb>());
    }
}
