using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.HomePage;

namespace B2C.Infrastructure.Persistence.Repositories
{
    public sealed class BannerEventRepository : IBannerEventRepository
    {
        private readonly B2CDbContext _db;

        public BannerEventRepository(B2CDbContext db) => _db = db;

        public async Task AddAsync(BannerEvent @event, CancellationToken ct = default)
            => await _db.BannerEvents.AddAsync(@event, ct);
    }
}
