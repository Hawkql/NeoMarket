using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Invoices
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct);

        /// <summary>Список накладных продавца с пагинацией.</summary>
        Task<(IReadOnlyCollection<Invoice> Items, int Total)> GetBySellerAsync(
            Guid sellerId,
            InvoiceStatus? statusFilter,
            int limit,
            int offset,
            CancellationToken ct);

        Task AddAsync(Invoice invoice, CancellationToken ct);
        /// <summary>Физическое удаление накладной (разрешено только в статусе Created).</summary>
        void Remove(Invoice invoice);
    }
}
