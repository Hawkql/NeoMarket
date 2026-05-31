using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence.Repositories
{

    public sealed class InvoiceRepository : IInvoiceRepository
    {
        private readonly B2BDbContext _dbContext;

        public InvoiceRepository(B2BDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            // Загружаем агрегат с позициями
            return await _dbContext.Invoices
                .Include(i=>i.Items)  // backing field, см. InvoiceConfiguration
                .FirstOrDefaultAsync(i => i.Id == id, ct);
        }

        public async Task<(IReadOnlyCollection<Invoice> Items, int Total)> GetBySellerAsync(
            Guid sellerId,
            InvoiceStatus? statusFilter,
            int limit,
            int offset,
            CancellationToken ct)
        {
            // Использует ix_invoices_seller_status_created
            var query = _dbContext.Invoices
                .AsNoTracking()
                .Where(i => i.SellerId == sellerId);

            if (statusFilter.HasValue)
                query = query.Where(i => i.Status == statusFilter.Value);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Include("_items")
                .ToListAsync(ct);

            return (items, total);
        }
        public void Remove(Invoice invoice)
        {
            _dbContext.Invoices.Remove(invoice);
        }
        public async Task AddAsync(Invoice invoice, CancellationToken ct)
        {
            await _dbContext.Invoices.AddAsync(invoice, ct);
        }
    }
}
