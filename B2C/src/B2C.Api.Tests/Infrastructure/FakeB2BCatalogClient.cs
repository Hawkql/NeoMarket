using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;
namespace B2C.Api.Tests.Infrastructure
{
    public sealed class FakeB2BCatalogClient : IB2BCatalogClient
    {
        private readonly Dictionary<Guid, SkuInfo> _skus = new();
        private readonly Dictionary<Guid, ProductSummary> _products = new();
        private readonly List<ProductSummary> _productList = new();         // для каталога/поиска/похожих
        private readonly Dictionary<Guid, ProductDetail> _productDetails = new();
        private readonly Dictionary<Guid, IReadOnlyList<Guid>> _categoryToProducts = new();
        private readonly List<CategoryNode> _categoryTree = new();
        private readonly Dictionary<Guid, IReadOnlyList<Breadcrumb>> _categoryBreadcrumbs = new();
        private readonly Dictionary<Guid, CategoryFilters> _categoryFilters = new();
        private bool _throwOnCatalog;

        // ===== test helpers =====
        public void SeedProduct(ProductSummary p) => _products[p.Id] = p;
        public void SeedSku(SkuInfo s) => _skus[s.Id] = s;
        public void SeedProductDetail(ProductDetail detail) => _productDetails[detail.Id] = detail;

        public void SeedProductInList(ProductSummary p)
        {
            _productList.Add(p);
            _products[p.Id] = p;
        }

        public void SeedCategoryTree(IEnumerable<CategoryNode> nodes)
        {
            _categoryTree.Clear();
            _categoryTree.AddRange(nodes);
        }

        public void SeedCategoryBreadcrumbs(Guid categoryId, IReadOnlyList<Breadcrumb> trail)
            => _categoryBreadcrumbs[categoryId] = trail;

        public void SeedCategoryFilters(Guid categoryId, CategoryFilters filters)
            => _categoryFilters[categoryId] = filters;

        public void MarkOutOfStock(Guid skuId)
        {
            if (_skus.TryGetValue(skuId, out var s))
                _skus[skuId] = s with { InStock = false };
        }

        public void RemoveProduct(Guid productId)
        {
            _products.Remove(productId);
            foreach (var id in _skus.Where(kv => kv.Value.ProductId == productId).Select(kv => kv.Key).ToList())
                _skus.Remove(id);
        }

        public void Reset()
        {
            _skus.Clear();
            _products.Clear();
            _productList.Clear();
            _productDetails.Clear();
            _categoryToProducts.Clear();
            _categoryTree.Clear();
            _categoryBreadcrumbs.Clear();
            _categoryFilters.Clear();
            _throwOnCatalog = false;
        }

        // ===== IB2BCatalogClient =====
        public Task<IReadOnlyList<SkuInfo>> GetSkusBatchAsync(IEnumerable<Guid> skuIds, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<SkuInfo>>(
                skuIds.Where(_skus.ContainsKey).Select(id => _skus[id]).ToList());

        public Task<IReadOnlyList<ProductSummary>> GetProductsBatchAsync(IEnumerable<Guid> productIds, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ProductSummary>>(
                productIds.Where(_products.ContainsKey).Select(id => _products[id]).ToList());


        public void MapProductToCategory(Guid productId, Guid categoryId)
        {
            if (!_categoryToProducts.TryGetValue(categoryId, out var list))
            {
                _categoryToProducts[categoryId] = new List<Guid> { productId };
            }
            else
            {
                ((List<Guid>)list).Add(productId);
            }
        }

        /// <summary>Имитация: B2B недоступен (для US-CAT-01 acceptance "502/503").</summary>
        public void ThrowOnCatalog() => _throwOnCatalog = true;

        public Task<CatalogPage> ListProductsAsync(CatalogQuery q, CancellationToken ct)
        {
            if (_throwOnCatalog)
                throw new System.Net.Http.HttpRequestException("B2B unavailable");

            IEnumerable<ProductSummary> result = _productList;

            // Фильтр по категории.
            if (q.CategoryId.HasValue
                && _categoryToProducts.TryGetValue(q.CategoryId.Value, out var ids))
            {
                var idSet = new HashSet<Guid>(ids);
                result = result.Where(p => idSet.Contains(p.Id));
            }

            // Текстовый поиск по Title.
            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var needle = q.Search.ToLowerInvariant();
                result = result.Where(p => p.Title.ToLowerInvariant().Contains(needle));
            }

            // Фильтр цены.
            if (q.MinPrice.HasValue) result = result.Where(p => p.Price >= q.MinPrice.Value);
            if (q.MaxPrice.HasValue) result = result.Where(p => p.Price <= q.MaxPrice.Value);

            // Сортировка.
            result = q.Sort switch
            {
                CatalogSort.PriceAsc => result.OrderBy(p => p.Price),
                CatalogSort.PriceDesc => result.OrderByDescending(p => p.Price),
                CatalogSort.Rating => result.OrderByDescending(p => p.Rating ?? 0),
                _ => result,
            };

            var materialised = result.ToList();
            var page = materialised.Skip(q.Offset).Take(q.Limit).ToList();

            return Task.FromResult(new CatalogPage(page, materialised.Count, q.Limit, q.Offset));
        }

        public Task<ProductDetail?> GetProductAsync(Guid productId, CancellationToken ct)
            => Task.FromResult(_productDetails.TryGetValue(productId, out var d) ? d : null);

        public Task<IReadOnlyList<ProductSummary>> GetSimilarAsync(Guid productId, int limit, CancellationToken ct)
        {
            if (!_products.ContainsKey(productId))
                return Task.FromResult<IReadOnlyList<ProductSummary>>(Array.Empty<ProductSummary>());

            // Все из того же category, кроме текущего.
            var category = _categoryToProducts.FirstOrDefault(kv => kv.Value.Contains(productId)).Key;
            if (category == Guid.Empty)
                return Task.FromResult<IReadOnlyList<ProductSummary>>(Array.Empty<ProductSummary>());

            var similar = _categoryToProducts[category]
                .Where(id => id != productId && _products.ContainsKey(id))
                .Take(limit)
                .Select(id => _products[id])
                .ToList();
            return Task.FromResult<IReadOnlyList<ProductSummary>>(similar);
        }

        public Task<CategoryFilters> GetCategoryFiltersAsync(Guid categoryId, CancellationToken ct)
            => Task.FromResult(_categoryFilters.TryGetValue(categoryId, out var f)
                ? f
                : new CategoryFilters(Array.Empty<FilterDefinition>(), null, null));

        public Task<CategoryFilters> GetFacetsAsync(CatalogQuery q, CancellationToken ct)
            => Task.FromResult(q.CategoryId.HasValue && _categoryFilters.TryGetValue(q.CategoryId.Value, out var f)
                ? f
                : new CategoryFilters(Array.Empty<FilterDefinition>(), null, null));

        public Task<IReadOnlyList<CategoryNode>> GetCategoryTreeAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<CategoryNode>>(_categoryTree.ToList());

        public Task<IReadOnlyList<Breadcrumb>> GetBreadcrumbsAsync(
            Guid? categoryId, Guid? productId, CancellationToken ct)
        {
            if (categoryId.HasValue && _categoryBreadcrumbs.TryGetValue(categoryId.Value, out var c))
                return Task.FromResult(c);
            return Task.FromResult<IReadOnlyList<Breadcrumb>>(Array.Empty<Breadcrumb>());
        }
    }
}
