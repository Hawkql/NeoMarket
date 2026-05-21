using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Images
{
    public interface IImageRepository
    {
        Task<Image?> GetByIdAsync(Guid id,CancellationToken ct);
        /// <summary>
        /// Получить все картинки entity (товара или SKU), отсортированные по Ordering.
        /// Используется в Query handlers для построения ProductResponse / SkuResponse.
        /// </summary>
        Task<IReadOnlyCollection<Image>> GetByEntityAsync(
            ImageEntityType entityType,
            Guid entityId,
            CancellationToken ct);
        /// <summary>
        /// Batch-версия для оптимизации (например, при загрузке списка товаров —
        /// одним запросом подгружаем все картинки для всех product_id сразу).
        /// </summary>
        Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Image>>> GetByEntitiesAsync(
            ImageEntityType entityType,
            IEnumerable<Guid> entityIds,
            CancellationToken ct);

        Task AddAsync(Image image,CancellationToken ct);
        void Remove(Image image);
    }
}
