using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Sellers;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence.Repositories
{
    public sealed class SellerRepository : ISellerRepository
    {
        private readonly B2BDbContext _dbContext;

        public SellerRepository(B2BDbContext dbContext) => _dbContext = dbContext;

        public async Task<Seller?> GetByIdAsync(Guid id, CancellationToken ct)
            => await _dbContext.Sellers.FirstOrDefaultAsync(s => s.Id == id, ct);

        public async Task<Seller?> GetByEmailAsync(string email, CancellationToken ct)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return await _dbContext.Sellers
                .FirstOrDefaultAsync(s => s.Email == normalized && !s.Deleted, ct);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return await _dbContext.Sellers
                .AsNoTracking()
                .AnyAsync(s => s.Email == normalized && !s.Deleted, ct);
        }

        public async Task AddAsync(Seller seller, CancellationToken ct)
            => await _dbContext.Sellers.AddAsync(seller, ct);
    }
}
