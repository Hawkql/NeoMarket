namespace B2C.Api.Contracts
{
    public sealed record B2BEventEnvelope(
       Guid IdempotencyKey,
       string EventType,           // "product_blocked", "product_deleted", "sku_out_of_stock"
       B2BEventData Data);

    /// <summary>
    /// Данные события. Поля опциональны — заполняются в зависимости от EventType.
    /// product_blocked/deleted: ProductId + SkuIds.
    /// sku_out_of_stock: ProductId + SkuId.
    /// </summary>
    public sealed record B2BEventData(
        Guid ProductId,
        Guid[]? SkuIds,
        Guid? SkuId);
}
