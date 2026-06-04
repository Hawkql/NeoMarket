namespace B2C.Api.Contracts
{
    /// <summary>
    /// openapi: OrderCreateRequest. required: address_id, payment_method_id.
    /// IdempotencyKey идёт ОТДЕЛЬНО в заголовке Idempotency-Key (см. Controller),
    /// не в теле — openapi прямо требует header.
    /// </summary>
    public sealed record CreateOrderRequest(
        Guid AddressId,
        Guid PaymentMethodId,
        string? Comment,
        List<CreateOrderItemRequest> Items);

    public sealed record CreateOrderItemRequest(
        Guid SkuId,
        int Quantity);

    public sealed record TransitionOrderStatusRequest(string TargetStatus);

    public sealed record CancelOrderRequest(string? Reason);

}