using B2B.Application.Invoices.Dtos;

namespace B2B.Api.Contracts
{
    public sealed record CreateInvoiceRequest(
    IReadOnlyList<InvoiceItemInputDto> Items);

    public sealed record AcceptInvoiceRequest(
        IReadOnlyList<AcceptInvoiceItemInputDto>? AcceptedItems);
}
