namespace B2C.Api.Contracts
{
    /// <summary>
    /// openapi-конверт события от B2B (схема B2BEvent).
    /// Required: event_type, idempotency_key, occurred_at, payload.
    /// EventType — UPPER_SNAKE_CASE enum, парсится строкой и роутится в контроллере.
    /// </summary>
    public sealed record B2BEventEnvelope(
        Guid IdempotencyKey,
        string EventType,            // PRODUCT_BLOCKED, PRODUCT_HARD_BLOCKED, PRODUCT_DELETED,
                                     // SKU_OUT_OF_STOCK, SKU_BACK_IN_STOCK, PRICE_CHANGED
        DateTime OccurredAt,
        B2BEventPayload Payload);

    /// <summary>
    /// Полиморфный payload. По openapi это oneOf(EventProductRef, EventSkuStock, EventPriceChanged),
    /// но т.к. System.Text.Json в .NET 8 не имеет первоклассной поддержки oneOf без кастом-конвертеров,
    /// используем discriminated DTO с опциональными полями. Контроллер по EventType отбирает нужные.
    /// 
    /// EventProductRef:   product_id, reason?
    /// EventSkuStock:     sku_id, product_id, available_quantity
    /// EventPriceChanged: sku_id, product_id, old_price, new_price
    /// </summary>
    public sealed record B2BEventPayload(
        Guid? ProductId,
        string? Reason,
        Guid? SkuId,
        int? AvailableQuantity,
        int? OldPrice,
        int? NewPrice);
}