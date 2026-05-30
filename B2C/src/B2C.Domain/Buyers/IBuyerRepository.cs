using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Buyers
{
    public interface IBuyerRepository
    {
        Task<Buyer?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Buyer?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
        Task AddAsync(Buyer buyer, CancellationToken ct = default);
        void Update(Buyer buyer);
    }
}
