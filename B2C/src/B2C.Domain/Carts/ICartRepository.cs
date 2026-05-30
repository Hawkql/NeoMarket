using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Carts
{
    public interface ICartRepository
    {
        Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken ct);

        /// <summary>
        /// Получить корзину авторизованного покупателя. Создаётся отдельно (lazy upsert)
        /// в Application слое: при первом обращении к /api/v1/cart, если корзины нет, создаётся пустая.
        /// </summary>
        Task<Cart?> GetByBuyerAsync(Guid buyerId, CancellationToken ct);

        Task<Cart?> GetBySessionAsync(string sessionId, CancellationToken ct);

        /// <summary>
        /// Найти все корзины (любых владельцев), в которых есть позиции с указанными SkuId.
        /// Используется обработчиком события от B2B (US-ORD-04): когда приходит SKU_OUT_OF_STOCK,
        /// нужно пометить позиции у ВСЕХ покупателей, а не у одного.
        /// </summary>
        Task<IReadOnlyList<Cart>> ListWithAnySkuAsync(IEnumerable<Guid> skuIds, CancellationToken ct);

        /// <summary>
        /// Найти все корзины, содержащие указанный товар.
        /// Для PRODUCT_BLOCKED/PRODUCT_DELETED от B2B.
        /// </summary>
        Task<IReadOnlyList<Cart>> ListWithProductAsync(Guid productId, CancellationToken ct);

        Task AddAsync(Cart cart, CancellationToken ct);
        void Update(Cart cart);
        void Remove(Cart cart);
    }
}
