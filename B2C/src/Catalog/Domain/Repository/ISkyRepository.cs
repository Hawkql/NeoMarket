using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repository
{
    public interface ISkuRepository
    {
        Task<List<Sku>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);

        Task<Sku?> GetByIdAsync(Guid productId, Guid skuId, CancellationToken ct = default);
    }
}
