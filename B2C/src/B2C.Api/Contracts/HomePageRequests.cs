namespace B2C.Api.Contracts
{
    public sealed record RecordBannerEventRequest(
        Guid BannerId,
        string Type);
}
