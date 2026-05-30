using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Subscriptions.Dtos;
using MediatR;

namespace B2C.Application.Subscriptions.Commands.Subscribe
{
    /// <summary>
    /// Подписка на товар. Если подписка уже существует — заменяем notify_on (upsert-семантика).
    /// Это удобнее для фронта: одной операцией можно "переподписаться с новыми параметрами".
    /// </summary>
    public sealed record SubscribeCommand(
        Guid ProductId,
        NotifyOnDto NotifyOn) : IRequest;
}
