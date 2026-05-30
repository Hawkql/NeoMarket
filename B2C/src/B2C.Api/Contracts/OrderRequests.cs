namespace B2C.Api.Contracts
{
    /// <summary>
    /// Checkout (US-ORD-01). IdempotencyKey — клиент генерирует UUID для защиты
    /// от двойного submit. Цены НЕ принимаем — фиксируем на сервере из B2B.
    /// </summary>
    public sealed record CreateOrderRequest(
        Guid IdempotencyKey,
        string DeliveryAddress,
        List<CreateOrderItemRequest> Items);

    public sealed record CreateOrderItemRequest(
        Guid SkuId,
        int Quantity);

    /// <summary>
    /// Admin/service переход статуса. TargetStatus — строка ("assembling", "delivering",
    /// "delivered"). Контроллер мапит в OrderStatusDto.
    /// </summary>
    public sealed record TransitionOrderStatusRequest(string TargetStatus);
}
