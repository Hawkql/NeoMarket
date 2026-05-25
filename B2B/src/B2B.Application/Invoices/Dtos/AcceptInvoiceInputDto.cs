using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Invoices.Dtos
{
    public sealed record AcceptInvoiceItemInputDto(Guid InvoiceItemId, int AcceptedQuantity);
}
