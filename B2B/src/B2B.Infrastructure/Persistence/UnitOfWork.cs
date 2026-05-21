using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;

namespace B2B.Infrastructure.Persistence
{

    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly B2BDbContext _dbContext;

        public UnitOfWork(B2BDbContext dbContext) => _dbContext = dbContext;

        public Task<int> SaveChangesAsync(CancellationToken ct)
            => _dbContext.SaveChangesAsync(ct);
    }
}
