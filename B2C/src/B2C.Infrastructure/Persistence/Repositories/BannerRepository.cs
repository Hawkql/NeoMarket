using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.HomePage;
using Microsoft.EntityFrameworkCore;

namespace B2C.Infrastructure.Persistence.Repositories
{
    public sealed class BannerRepository : IBannerRepository
    {
        private readonly B2CDbContext _db;

        public BannerRepository(B2CDbContext db) => _db = db;

        public async Task<Banner?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Banners.FirstOrDefaultAsync(b => b.Id == id, ct);

        /// <summary>
        /// Видимые баннеры на момент now, отсортированные Priority DESC.
        /// Условие IsVisibleAt разворачиваем в SQL-фильтр (не вызываем доменный метод
        /// в LINQ-to-Entities — он не транслируется в SQL).
        /// </summary>
        public async Task<IReadOnlyList<Banner>> ListVisibleAsync(DateTime now, CancellationToken ct = default)
            => await _db.Banners
                .AsNoTracking()
                .Where(b => b.IsActive
                    && (b.StartsAt == null || b.StartsAt <= now)
                    && (b.EndsAt == null || b.EndsAt >= now))
                .OrderByDescending(b => b.Priority)
                .ToListAsync(ct);

        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
            => await _db.Banners.AsNoTracking().AnyAsync(b => b.Id == id, ct);

        public async Task AddAsync(Banner banner, CancellationToken ct = default)
            => await _db.Banners.AddAsync(banner, ct);

        public void Update(Banner banner) => _db.Banners.Update(banner);

        public void Remove(Banner banner) => _db.Banners.Remove(banner);
    }
}
