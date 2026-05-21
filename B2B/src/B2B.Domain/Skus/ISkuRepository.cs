using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        Task<int> CountByProductIdAsync(Guid productId, CancellationToken ct);

        Task AddAsync(Sku sku, CancellationToken ct);

        /// <summary>Физическое удаление (если потребуется). Soft-delete — через Sku.MarkAsDeleted.</summary>
        void Remove(Sku sku);
    }
}
