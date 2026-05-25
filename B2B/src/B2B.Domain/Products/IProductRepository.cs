using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Products
{
    public interface IProductRepository
    {
        Task<(IReadOnlyCollection<Product> Items, int Total)> GetPublicCatalogAsync(
    PublicCatalogFilter filter, CancellationToken ct);
        Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);

        /// <summary>Список товаров продавца с пагинацией (для GET /api/v1/products в режиме seller).</summary>
        Task<(IReadOnlyCollection<Product> Items, int Total)> GetBySellerAsync(
            Guid sellerId,
            ProductStatus? statusFilter,
            bool includeDeleted,
            int limit,
            int offset,
            CancellationToken ct);

        Task<IReadOnlyCollection<Product>> GetPublicByIdsAsync(
            IEnumerable<Guid> productIds, CancellationToken ct);

        Task AddAsync(Product product, CancellationToken ct);

        /// <summary>Soft-delete на уровне Domain (Product.MarkAsDeleted). Этот метод — для физического удаления.</summary>
        void Remove(Product product);
        /// <summary>
        /// Похожие витринные товары: та же категория, кроме самого товара,
        /// только Moderated + не deleted + с живым SKU. Случайный порядок, ограничение limit.
        /// </summary>
        Task<IReadOnlyCollection<Product>> GetSimilarAsync(
            Guid productId, Guid categoryId, int limit, CancellationToken ct);
    }
}
