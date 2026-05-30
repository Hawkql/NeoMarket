using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.HomePage;
using Microsoft.EntityFrameworkCore;

namespace B2C.Infrastructure.Persistence.Repositories
{

    public sealed class CollectionRepository : ICollectionRepository
    {
        private readonly B2CDbContext _db;

        public CollectionRepository(B2CDbContext db) => _db = db;

        public async Task<Collection?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Collections.FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<Collection?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            var normalized = slug.Trim().ToLowerInvariant();
            return await _db.Collections.FirstOrDefaultAsync(c => c.Slug == normalized, ct);
        }

        public async Task<IReadOnlyList<Collection>> ListActiveAsync(CancellationToken ct = default)
            => await _db.Collections
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.Priority)
                .ToListAsync(ct);

        public async Task AddAsync(Collection collection, CancellationToken ct = default)
            => await _db.Collections.AddAsync(collection, ct);

        public void Update(Collection collection) => _db.Collections.Update(collection);

        public void Remove(Collection collection) => _db.Collections.Remove(collection);
    }
}
