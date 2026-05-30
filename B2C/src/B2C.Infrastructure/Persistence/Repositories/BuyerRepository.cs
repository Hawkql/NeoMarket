using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Buyers;
using Microsoft.EntityFrameworkCore;
namespace B2C.Infrastructure.Persistence.Repositories
{
    public sealed class BuyerRepository : IBuyerRepository
    {
        private readonly B2CDbContext _db;

        public BuyerRepository(B2CDbContext db) => _db = db;

        public async Task<Buyer?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Buyers.FirstOrDefaultAsync(b => b.Id == id, ct);

        public async Task<Buyer?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return await _db.Buyers
                .FirstOrDefaultAsync(b => b.Email == normalized && !b.Deleted, ct);
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return await _db.Buyers
                .AsNoTracking()
                .AnyAsync(b => b.Email == normalized && !b.Deleted, ct);
        }

        public async Task AddAsync(Buyer buyer, CancellationToken ct = default)
            => await _db.Buyers.AddAsync(buyer, ct);

        public void Update(Buyer buyer) => _db.Buyers.Update(buyer);
    }
}
