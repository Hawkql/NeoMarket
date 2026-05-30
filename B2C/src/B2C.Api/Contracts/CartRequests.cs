namespace B2C.Api.Contracts
{
    public sealed record AddToCartRequest(
         Guid SkuId,
         int Quantity);

    public sealed record UpdateCartItemRequest(
        int Quantity);
}
