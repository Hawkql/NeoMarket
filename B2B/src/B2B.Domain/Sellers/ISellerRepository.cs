using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Sellers
{
    public interface ISellerRepository
    {
        Task<Seller?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Seller?> GetByEmailAsync(string email, CancellationToken ct);
        Task<bool> EmailExistsAsync(string email, CancellationToken ct);
        Task AddAsync(Seller seller, CancellationToken ct);
    }
}
