using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Sellers;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence.Repositories
{
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly B2BDbContext _dbContext;

        public RefreshTokenRepository(B2BDbContext dbContext) => _dbContext = dbContext;

        public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct)
            => await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        // tracked (без AsNoTracking) — нужен для Revoke + SaveChanges при ротации

        public async Task AddAsync(RefreshToken token, CancellationToken ct)
            => await _dbContext.RefreshTokens.AddAsync(token, ct);
    }
}
