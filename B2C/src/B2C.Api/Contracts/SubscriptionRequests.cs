namespace B2C.Api.Contracts
{
    public sealed record SubscribeRequest(string[] NotifyOn);

    public sealed record UpdateSubscriptionRequest(string[] NotifyOn);
}
