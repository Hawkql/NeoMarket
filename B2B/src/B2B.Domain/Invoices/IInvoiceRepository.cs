using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Invoices
{
    public interface IInvoiceRepository
    { 
        Task<Invoice>? GetByIdAsync(Guid id,CancellationToken ct);
        Task AddAsync(Invoice invoice,CancellationToken ct);
        void Update(Invoice invoice);
    }
}
