using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Addresses
{
    public interface IAddressRepository
    {
        Task<Address?> GetByIdForBuyerAsync(Guid addressId, Guid buyerId, CancellationToken ct = default);
        Task<IReadOnlyList<Address>> ListByBuyerAsync(Guid buyerId, CancellationToken ct = default);
        Task AddAsync(Address address, CancellationToken ct = default);
        void Update(Address address);
        void Remove(Address address);

        /// <summary>
        /// Сбросить флаг IsDefault у всех адресов покупателя, кроме указанного.
        /// Гарантирует инвариант "не больше одного default-адреса на покупателя".
        /// </summary>
        Task UnsetDefaultExceptAsync(Guid buyerId, Guid exceptAddressId, CancellationToken ct = default);
    }
}
