using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;

using B2C.Infrastructure.Integration.Contracts;
using Microsoft.Extensions.Logging;

namespace B2C.Infrastructure.Integration
{
    public sealed class B2BCatalogHttpClient : IB2BCatalogClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<B2BCatalogHttpClient> _logger;

        public B2BCatalogHttpClient(HttpClient http, ILogger<B2BCatalogHttpClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<CatalogPage> ListProductsAsync(CatalogQuery query, CancellationToken ct)
        {
            // Префикс /public/ — B2B держит публичный каталог отдельно от приватного
            // (приватный /api/v1/products — для продавцов с JWT).
            var url = BuildCatalogUrl("/api/v1/public/products", query);
            var response = await _http.GetFromJsonAsync<B2bCatalogPageResponse>(url, ct)
                ?? new B2bCatalogPageResponse();
            return B2BHttpMapper.ToCatalogPage(response);
        }

        public async Task<ProductDetail?> GetProductAsync(Guid productId, CancellationToken ct)
        {
            var response = await _http.GetAsync($"/api/v1/public/products/{productId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var detail = await response.Content.ReadFromJsonAsync<B2bProductDetailResponse>(ct);
            return detail is null ? null : B2BHttpMapper.ToProductDetail(detail);
        }

        public async Task<IReadOnlyList<ProductSummary>> GetProductsBatchAsync(
            IEnumerable<Guid> productIds, CancellationToken ct)
        {
            var ids = productIds.ToList();
            if (ids.Count == 0) return Array.Empty<ProductSummary>();

            var response = await _http.PostAsJsonAsync(
                "/api/v1/public/products/batch", new { product_ids = ids }, ct);
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<List<B2bProductSummaryResponse>>(ct)
                ?? new List<B2bProductSummaryResponse>();

            return items.Select(B2BHttpMapper.ToProductSummary).ToList();
        }

        public async Task<IReadOnlyList<SkuInfo>> GetSkusBatchAsync(
            IEnumerable<Guid> skuIds, CancellationToken ct)
        {
            var ids = skuIds.ToList();
            if (ids.Count == 0) return Array.Empty<SkuInfo>();

            // B2B не предоставляет batch-endpoint для SKU — только GET /api/v1/public/skus/{id}.
            // Делаем fan-out параллельно через Task.WhenAll, фильтруем 404 (удалённые/несуществующие).
            //
            // ВРЕМЕННОЕ РЕШЕНИЕ. Если объём вызовов вырастет — попросить B2B добавить
            // POST /api/v1/public/skus/batch и заменить здесь на один HTTP-запрос.
            var tasks = ids.Select(id => GetSkuOrNullAsync(id, ct));
            var results = await Task.WhenAll(tasks);
            return results.Where(r => r is not null).Select(r => r!).ToList();
        }

        public async Task<IReadOnlyList<ProductSummary>> GetSimilarAsync(
            Guid productId, int limit, CancellationToken ct)
        {
            var items = await _http.GetFromJsonAsync<List<B2bProductSummaryResponse>>(
                $"/api/v1/products/{productId}/similar?limit={limit}", ct)
                ?? new List<B2bProductSummaryResponse>();

            return items.Select(B2BHttpMapper.ToProductSummary).ToList();
        }

        public async Task<CategoryFilters> GetCategoryFiltersAsync(Guid categoryId, CancellationToken ct)
        {
            var response = await _http.GetFromJsonAsync<B2bCategoryFiltersResponse>(
                $"/api/v1/categories/{categoryId}/filters", ct)
                ?? new B2bCategoryFiltersResponse();
            return B2BHttpMapper.ToCategoryFilters(response);
        }

        public async Task<CategoryFilters> GetFacetsAsync(CatalogQuery query, CancellationToken ct)
        {
            var url = BuildCatalogUrl("/api/v1/products/facets", query);
            var response = await _http.GetFromJsonAsync<B2bCategoryFiltersResponse>(url, ct)
                ?? new B2bCategoryFiltersResponse();
            return B2BHttpMapper.ToCategoryFilters(response);
        }

        public async Task<IReadOnlyList<CategoryNode>> GetCategoryTreeAsync(CancellationToken ct)
        {
            var nodes = await _http.GetFromJsonAsync<List<B2bCategoryNodeResponse>>(
                "/api/v1/categories", ct)
                ?? new List<B2bCategoryNodeResponse>();

            return nodes.Select(B2BHttpMapper.ToCategoryNode).ToList();
        }

        public async Task<IReadOnlyList<Breadcrumb>> GetBreadcrumbsAsync(
            Guid? categoryId, Guid? productId, CancellationToken ct)
        {
            var queryParam = categoryId.HasValue
                ? $"category_id={categoryId}"
                : $"product_id={productId}";

            var crumbs = await _http.GetFromJsonAsync<List<B2bBreadcrumbResponse>>(
                $"/api/v1/breadcrumbs?{queryParam}", ct)
                ?? new List<B2bBreadcrumbResponse>();

            return crumbs.Select(B2BHttpMapper.ToBreadcrumb).ToList();
        }

        /// <summary>
        /// Строит query string для каталога из CatalogQuery.
        /// Фильтры (Dictionary) разворачиваются как filter[slug]=value.
        /// </summary>
        private static string BuildCatalogUrl(string basePath, CatalogQuery q)
        {
            var parts = new List<string>
            {
                $"limit={q.Limit}",
                $"offset={q.Offset}",
                $"sort={MapSort(q.Sort)}",
            };

            if (q.CategoryId.HasValue) parts.Add($"category_id={q.CategoryId}");
            if (!string.IsNullOrWhiteSpace(q.Search))
                parts.Add($"search={Uri.EscapeDataString(q.Search)}");
            if (q.MinPrice.HasValue) parts.Add($"min_price={q.MinPrice}");
            if (q.MaxPrice.HasValue) parts.Add($"max_price={q.MaxPrice}");

            if (q.Filters is not null)
                foreach (var (slug, value) in q.Filters)
                    parts.Add($"filter[{Uri.EscapeDataString(slug)}]={Uri.EscapeDataString(value)}");

            return $"{basePath}?{string.Join("&", parts)}";
        }
        private async Task<SkuInfo?> GetSkuOrNullAsync(Guid skuId, CancellationToken ct)
        {
            var response = await _http.GetAsync($"/api/v1/public/skus/{skuId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<B2bSkuResponse>(ct);
            return dto is null ? null : B2BHttpMapper.ToSkuInfo(dto);
        }
        private static string MapSort(CatalogSort sort) => sort switch
        {
            CatalogSort.Rating => "rating",
            CatalogSort.Popularity => "popularity",
            CatalogSort.PriceAsc => "price_asc",
            CatalogSort.PriceDesc => "price_desc",
            CatalogSort.DateDesc => "date_desc",
            CatalogSort.DiscountDesc => "discount_desc",
            _ => "rating",
        };
    }
}
