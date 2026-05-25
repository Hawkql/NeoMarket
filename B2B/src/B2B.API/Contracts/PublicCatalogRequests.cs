namespace B2B.Api.Contracts
{
    public sealed record BatchProductsRequest(
    IReadOnlyList<Guid> ProductIds);
}
