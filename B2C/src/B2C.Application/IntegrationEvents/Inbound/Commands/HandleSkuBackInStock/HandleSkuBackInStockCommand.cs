using System;
using MediatR;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleSkuBackInStock
{
    /// <summary>
    /// SKU_BACK_IN_STOCK от B2B — SKU снова в наличии.
    /// По openapi триггерит уведомления подписчикам (Subscriptions с notify_on=BACK_IN_STOCK).
    /// В MVP — Inbox-регистрация + лог; реальная отправка уведомлений — расширение через Outbox.
    /// </summary>
    public sealed record HandleSkuBackInStockCommand(
        Guid IdempotencyKey,
        Guid ProductId,
        Guid SkuId,
        int AvailableQuantity) : IRequest;
}