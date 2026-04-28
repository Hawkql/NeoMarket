using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Products
{
    public  interface IProductRepository
    {
        Task<Product?> GetByIdProduct(Guid id, CancellationToken ct);
        Task AddAsync(Product product,CancellationToken ct);
        Task<Product?> GetByIdSkuIdAsync(Guid skuId, CancellationToken ct);
        void Update(Product product);
    }
}
