using System;
using MediatR;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductHardBlocked
{
    /// <summary>
    /// PRODUCT_HARD_BLOCKED от B2B — продавец заблокирован/санкции/etc, товар недоступен безусловно.
    /// Семантически идентично ProductBlocked для B2C — помечаем корзины как недоступные.
    /// Различие — в Source/EventType для аудита: Inbox хранит тип отдельно.
    /// </summary>
    public sealed record HandleProductHardBlockedCommand(
        Guid IdempotencyKey,
        Guid ProductId,
        string? Reason) : IRequest;
}