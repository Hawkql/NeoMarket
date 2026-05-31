using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace B2B.Domain.Inventory
{
    public interface IInventoryReservationRepository
    {
        Task AddRangeAsync(IEnumerable<InventoryReservation> reservations, CancellationToken ct);

        /// <summary>Все резервы для заказа. Пустой список если резервов нет.</summary>
        Task<IReadOnlyCollection<InventoryReservation>> GetByOrderAsync(Guid orderId, CancellationToken ct);

        void RemoveRange(IEnumerable<InventoryReservation> reservations);
    }
}