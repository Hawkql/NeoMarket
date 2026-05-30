using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Subscriptions
{
    public interface IProductSubscriptionRepository
    {
        Task<ProductSubscription?> GetAsync(Guid buyerId, Guid productId, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid buyerId, Guid productId, CancellationToken ct = default);
        Task<IReadOnlyList<ProductSubscription>> ListByBuyerAsync(Guid buyerId, CancellationToken ct = default);

        Task AddAsync(ProductSubscription sub, CancellationToken ct = default);
        void Remove(ProductSubscription sub);
    }
}
