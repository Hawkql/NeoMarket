using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Addresses;

using Microsoft.EntityFrameworkCore;

namespace B2C.Infrastructure.Persistence.Repositories
{
    public sealed class AddressRepository : IAddressRepository
    {
        private readonly B2CDbContext _db;

        public AddressRepository(B2CDbContext db) => _db = db;

        public async Task<Address?> GetByIdForBuyerAsync(
            Guid addressId, Guid buyerId, CancellationToken ct = default)
            => await _db.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.BuyerId == buyerId, ct);

        public async Task<IReadOnlyList<Address>> ListByBuyerAsync(
            Guid buyerId, CancellationToken ct = default)
            => await _db.Addresses
                .Where(a => a.BuyerId == buyerId)
                .ToListAsync(ct);

        public async Task AddAsync(Address address, CancellationToken ct = default)
            => await _db.Addresses.AddAsync(address, ct);

        public void Update(Address address) => _db.Addresses.Update(address);

        public void Remove(Address address) => _db.Addresses.Remove(address);

        /// <summary>
        /// Сбросить is_default у всех адресов покупателя, кроме указанного.
        /// Bulk-update одним запросом — поддержка инварианта "один default".
        /// </summary>
        public async Task UnsetDefaultExceptAsync(
            Guid buyerId, Guid exceptAddressId, CancellationToken ct = default)
        {
            await _db.Addresses
                .Where(a => a.BuyerId == buyerId && a.Id != exceptAddressId && a.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), ct);
        }
    }
}
