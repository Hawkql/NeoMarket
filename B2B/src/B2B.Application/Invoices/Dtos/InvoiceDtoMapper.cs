using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Invoices;

namespace B2B.Application.Invoices.Dtos
{
    internal static class InvoiceDtoMapper
    {
        public static InvoiceResponseDto Map(Invoice invoice)
        {
            return new InvoiceResponseDto(
                Id: invoice.Id,
                SellerId: invoice.SellerId,
                Status: invoice.Status,
                Items: invoice.Items
                    .Select(i => new InvoiceItemResponseDto(
                        Id: i.Id,
                        SkuId: i.SkuId,
                        Quantity: i.Quantity,
                        AcceptedQuantity: i.AcceptedQuantity ?? 0))
                    .ToList(),
                CreatedAt: invoice.CreatedAt,
                UpdatedAt: invoice.UpdatedAt,
                AcceptedAt: invoice.AcceptedAt,
                AcceptedBy: invoice.AcceptedBy);
        }
    }
}
