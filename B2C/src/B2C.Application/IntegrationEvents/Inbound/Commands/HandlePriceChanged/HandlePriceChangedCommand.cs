using System;
using MediatR;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandlePriceChanged
{
    /// <summary>
    /// PRICE_CHANGED от B2B — цена SKU изменилась.
    /// По openapi обновляет кеш каталога + триггерит PRICE_DROP-уведомления подписчикам.
    /// MVP — Inbox + лог; кеш каталога и Outbox-уведомления — расширение.
    /// </summary>
    public sealed record HandlePriceChangedCommand(
        Guid IdempotencyKey,
        Guid ProductId,
        Guid SkuId,
        int OldPrice,
        int NewPrice) : IRequest;
}