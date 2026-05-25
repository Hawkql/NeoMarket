using B2B.Application.Inventory.Dtos;

namespace B2B.Api.Contracts
{
    public sealed record ReserveRequest(
     Guid IdempotencyKey,
     Guid OrderId,
     IReadOnlyList<InventoryItemDto> Items);

    public sealed record InventoryOrderRequest(
        Guid OrderId,
        IReadOnlyList<InventoryItemDto> Items);
}
