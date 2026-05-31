using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using B2B.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence.Repositories
{
    public sealed class InventoryReservationRepository : IInventoryReservationRepository
    {
        private readonly B2BDbContext _db;

        public InventoryReservationRepository(B2BDbContext db) => _db = db;

        public async Task AddRangeAsync(
            IEnumerable<InventoryReservation> reservations, CancellationToken ct)
        {
            await _db.Set<InventoryReservation>().AddRangeAsync(reservations, ct);
        }

        public async Task<IReadOnlyCollection<InventoryReservation>> GetByOrderAsync(
            Guid orderId, CancellationToken ct)
        {
            return await _db.Set<InventoryReservation>()
                .Where(r => r.OrderId == orderId)
                .ToListAsync(ct);
        }

        public void RemoveRange(IEnumerable<InventoryReservation> reservations)
        {
            _db.Set<InventoryReservation>().RemoveRange(reservations);
        }
    }
}