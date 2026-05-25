using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Invoices;

namespace B2B.Application.Invoices.Dtos
{
    public sealed record InvoiceItemInputDto(Guid SkuId, int Quantity);

    public sealed record InvoiceResponseDto(
        Guid Id,
        Guid SellerId,
        InvoiceStatus Status,
        IReadOnlyList<InvoiceItemResponseDto> Items,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? AcceptedAt,
        Guid? AcceptedBy);

    public sealed record InvoiceItemResponseDto(
        Guid Id,
        Guid SkuId,
        int Quantity,
        int AcceptedQuantity);   // null в Domain → 0 наружу (спека: required)
}
