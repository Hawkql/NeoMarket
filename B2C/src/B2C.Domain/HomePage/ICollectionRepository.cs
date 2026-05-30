using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.HomePage
{
    public interface ICollectionRepository
    {
        Task<Collection?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Collection?> GetBySlugAsync(string slug, CancellationToken ct = default);

        /// <summary>
        /// Активные подборки, отсортированные по Priority DESC.
        /// БЕЗ списка товаров (для US-CART-05: список подборок без products внутри).
        /// </summary>
        Task<IReadOnlyList<Collection>> ListActiveAsync(CancellationToken ct = default);

        Task AddAsync(Collection collection, CancellationToken ct = default);
        void Update(Collection collection);
        void Remove(Collection collection);
    }
}
