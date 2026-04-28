using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repository
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<(List<Product> Items, int TotalCount)> ListAsync(
            Guid? categoryId,
            string? search,
            Dictionary<string, string>? filters,
            string? sort,
            int limit,
            int offset,
            CancellationToken ct = default);

        Task<(List<Product> Items, int TotalCount)> GetSimilarAsync(
            Guid productId,
            Guid categoryId,
            int limit,
            int offset,
            CancellationToken ct = default);
    }
}
