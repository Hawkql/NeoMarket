using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.IntegrationEvents
{
    /// <summary>
    /// Базовый record для всех межсервисных событий (входящих и исходящих).
    /// 
    /// IdempotencyKey — для дедупликации в Inbox (входящие) и Outbox (исходящие).
    /// OccurredOnUtc — момент возникновения события на стороне источника.
    /// 
    /// Стиль наследников: `required init` properties + версия в имени класса (V1, V2, ...).
    /// </summary>
    public abstract record IntegrationEvent
    {
        public Guid IdempotencyKey { get; init; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    }
}
