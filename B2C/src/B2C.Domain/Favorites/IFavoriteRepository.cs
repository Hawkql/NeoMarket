using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Favorites
{
    public interface IFavoriteRepository
    {
        Task<Favorite?> GetAsync(Guid buyerId, Guid productId, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid buyerId, Guid productId, CancellationToken ct = default);

        /// <summary>
        /// Список ProductId покупателя — лёгкий метод для случаев, когда нужны только UUID
        /// (например, для проверки "is in favorites" при отображении каталога).
        /// </summary>
        Task<IReadOnlyList<Guid>> ListProductIdsByBuyerAsync(Guid buyerId, CancellationToken ct = default);

        /// <summary>
        /// Полные Favorite-агрегаты покупателя — для отображения списка с CreatedAt (AddedAt).
        /// </summary>
        Task<IReadOnlyList<Favorite>> ListByBuyerAsync(Guid buyerId, CancellationToken ct = default);

        Task AddAsync(Favorite favorite, CancellationToken ct = default);
        void Remove(Favorite favorite);

        /// <summary>
        /// Bulk-удаление всех Favorite записей по product_id (cross-buyer).
        /// Используется при ProductDeleted от B2B.
        /// </summary>
        Task RemoveAllByProductAsync(Guid productId, CancellationToken ct = default);
    }
}
