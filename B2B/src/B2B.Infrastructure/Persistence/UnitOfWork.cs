using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.common.Interface;

namespace B2B.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        
        private readonly B2BDbContext _db;
        public UnitOfWork( B2BDbContext db)
        {
            _db=db; 
        }
        public Task<int> SaveChangesAsync(CancellationToken ct)=>_db.SaveChangesAsync(ct);

    }
}
