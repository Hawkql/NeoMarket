namespace B2C.Api.Contracts
{
    /// <summary>
    /// openapi: requestBody.required = false.
    /// events: enum [BACK_IN_STOCK, PRICE_DROP], default — оба.
    /// Если тело не пришло / events == null / пустой массив — подразумеваем дефолт.
    /// </summary>
    public sealed record SubscribeRequest(string[]? Events);
}