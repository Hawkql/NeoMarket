using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Products;

namespace B2B.Domain.Skus
{
    public  interface ISkuRepository
    {
        Task<Sku?> GetByIdAsync(Guid Id, CancellationToken ct);
        /// <summary>SELECT FOR UPDATE — для Reserve/Unreserve/Fulfill.</summary>
        Task<Sku?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);

        /// <summary>Batch-получение SKU с блокировкой для all-or-nothing reserve.</summary>
        Task<IReadOnlyCollection<Sku>> GetByIdsForUpdateAsync(
            IEnumerable<Guid> ids, CancellationToken ct);

        Task<IReadOnlyCollection<Sku>> GetByProductIdAsync(Guid productId, CancellationToken ct);
        /// <summary>Минимальная цена активного SKU по каждому product_id (batch, для списков).</summary>
        Task<IReadOnlyDictionary<Guid, int>> GetMinPriceByProductIdsAsync(
            IEnumerable<Guid> productIds, CancellationToken ct);
        Task<int> CountByProductIdAsync(Guid productId, CancellationToken ct);

        Task AddAsync(Sku sku, CancellationToken ct);

        /// <summary>
        /// Возвращает sku_id → seller_id (через product) для переданных SKU.
        /// Используется для ownership-проверки при создании накладной (US-B2B-06).
        /// Удалённые SKU не включаются.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, Guid>> GetSellerIdsBySkuIdsAsync(
            IEnumerable<Guid> skuIds, CancellationToken ct);

        /// <summary>Физическое удаление (если потребуется). Soft-delete — через Sku.MarkAsDeleted.</summary>
        void Remove(Sku sku);

        /// <summary>Не удалённые SKU по списку product_id (batch, для витринных карточек).</summary>
        Task<IReadOnlyCollection<Sku>> GetByProductIdsAsync(
            IEnumerable<Guid> productIds, CancellationToken ct);

        Task<IReadOnlyDictionary<Guid, (Guid SellerId, ProductStatus ProductStatus)>>
    GetOwnerAndStatusBySkuIdsAsync(IEnumerable<Guid> skuIds, CancellationToken ct);

        Task<IReadOnlyDictionary<Guid, (int SkusCount, int TotalActiveQuantity)>>
                GetSkuStatsByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct);
    }
}
