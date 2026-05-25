using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Sellers
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
        Task AddAsync(RefreshToken token, CancellationToken ct);
    }
}
