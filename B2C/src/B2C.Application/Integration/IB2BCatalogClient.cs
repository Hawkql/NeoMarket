using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration.Dtos;


namespace B2C.Application.Integration
{
    /// <summary>
    /// Порт для read-операций к B2B-каталогу.
    /// 
    /// Реализация в Infrastructure делает HTTP-запросы с заголовком X-Service-Key
    /// (auth МЕЖДУ сервисами — это отдельная история от purchaser JWT нашего сервиса).
    /// 
    /// Возвращает нейтральные B2C-собственные DTO (Anti-Corruption Layer):
    /// без полей продавца, без cost_price, без reserved_quantity (US-CAT-03 на уровне типов).
    /// </summary>
    public interface IB2BCatalogClient
    {
        /// <summary>US-CAT-01: каталог с фильтрами/сортировкой/пагинацией.</summary>
        Task<CatalogPage> ListProductsAsync(CatalogQuery query, CancellationToken ct);

        /// <summary>US-CAT-03: карточка товара. null если не найден или не MODERATED.</summary>
        Task<ProductDetail?> GetProductAsync(Guid productId, CancellationToken ct);

        /// <summary>
        /// Batch-обогащение: Favorites, Cart, Collections отдают список UUID,
        /// B2B возвращает summary по этим товарам. Порядок результата НЕ гарантируется
        /// (вызывающий маппит обратно по Id).
        /// </summary>
        Task<IReadOnlyList<ProductSummary>> GetProductsBatchAsync(
            IEnumerable<Guid> productIds, CancellationToken ct);

        /// <summary>
        /// Batch обогащение SKU (для корзины: цена + наличие конкретного варианта).
        /// </summary>
        Task<IReadOnlyList<SkuInfo>> GetSkusBatchAsync(
            IEnumerable<Guid> skuIds, CancellationToken ct);

        /// <summary>US-CAT-04: похожие товары.</summary>
        Task<IReadOnlyList<ProductSummary>> GetSimilarAsync(
            Guid productId, int limit, CancellationToken ct);

        /// <summary>US-CAT-01: доступные фильтры для категории + диапазон цены.</summary>
        Task<CategoryFilters> GetCategoryFiltersAsync(Guid categoryId, CancellationToken ct);

        /// <summary>
        /// US-CAT-01: фасеты с подсчётом количества для текущей выборки.
        /// Отличается от GetCategoryFiltersAsync тем, что считает фасеты после применения
        /// уже выбранных фильтров (динамический фасеточный поиск).
        /// </summary>
        Task<CategoryFilters> GetFacetsAsync(CatalogQuery query, CancellationToken ct);

        /// <summary>US-CAT-05: дерево категорий (рекурсивно от корня).</summary>
        Task<IReadOnlyList<CategoryNode>> GetCategoryTreeAsync(CancellationToken ct);

        /// <summary>
        /// US-CAT-05: хлебные крошки от корня до целевой категории либо до категории товара.
        /// Один из двух параметров должен быть задан, не оба.
        /// </summary>
        Task<IReadOnlyList<Breadcrumb>> GetBreadcrumbsAsync(
            Guid? categoryId, Guid? productId, CancellationToken ct);
    }
}
