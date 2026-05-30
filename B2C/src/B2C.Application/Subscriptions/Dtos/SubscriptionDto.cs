using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Subscriptions.Dtos
{
    /// <summary>
    /// Подписка покупателя с обогащёнными данными о товаре.
    /// Возвращается в GET /favorites/subscriptions.
    /// </summary>
    public sealed record SubscriptionDto(
        Guid SubscriptionId,
        Guid ProductId,
        string Title,
        string? ImageUrl,
        int Price,
        bool InStock,
        NotifyOnDto NotifyOn,
        DateTime SubscribedAt);
}
