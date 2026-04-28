using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repository
{
    public sealed class SkuRepository(CatalogDbContext db) : ISkuRepository
    {
        public async Task<List<Sku>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        {
            return await db.Skus
                .AsNoTracking()
                .Where(s => s.ProductId == productId)
                .Include(s => s.Images.OrderBy(i => i.Order))
                .Include(s => s.Characteristics)
                .ToListAsync(ct);
        }

        public async Task<Sku?> GetByIdAsync(Guid productId, Guid skuId, CancellationToken ct = default)
        {
            return await db.Skus
                .AsNoTracking()
                .Include(s => s.Images.OrderBy(i => i.Order))
                .Include(s => s.Characteristics)
                .FirstOrDefaultAsync(s => s.Id == skuId && s.ProductId == productId, ct);
        }
    }
}
