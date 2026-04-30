using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly B2BDbContext _db;
        public ProductRepository(B2BDbContext db)
        {
                _db = db;
        }
        public async Task AddAsync(Product product, CancellationToken ct)
        {
            await _db.AddAsync(product, ct);
        }

        public async Task<Product?> GetByIdProduct(Guid id, CancellationToken ct)
        {
             return await _db.Products
                .Include(p => p.Skus).ThenInclude(s=>s.Characteristics)
                .Include(i=>i.Images)
                .FirstOrDefaultAsync(p=>p.Id == id,ct);
        }

        public void Update(Product product)=>_db.Products.Update(product);
        public Task<Product?> GetByIdSkuIdAsync(Guid skuId, CancellationToken ct) =>
            _db.Products.Include(p => p.Skus).FirstOrDefaultAsync(p => p.Skus.Any(s => s.Id == skuId),ct);
    }
}
