using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence.Repositories
{
    internal class InvoiceRepository : IInvoiceRepository
    {
        private readonly B2BDbContext _db;

        public InvoiceRepository(B2BDbContext db)
        {
            _db = db;
        }

        public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct) =>
            _db.Invoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

        public async Task AddAsync(Invoice invoice, CancellationToken ct)
        {
            await _db.Invoices.AddAsync(invoice, ct);
        }

        public void Update(Invoice invoice)
        {
            _db.Invoices.Update(invoice);
        }
    }
}
