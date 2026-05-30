using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Domain.Common
{
    public abstract record DomainEvent(DateTime OccurredOn) : INotification
    {
        /// <summary>
        /// Уникальный ID события. Используется для:
        /// - дедупликации в Inbox (events-таблица с unique-индексом на EventId);
        /// - корреляции в логах между сервисами;
        /// - идемпотентности обработчиков.
        /// </summary>
        public Guid EventId { get; init; } = Guid.NewGuid();

        protected DomainEvent() : this(DateTime.UtcNow) { }

        /// <summary>Когда событие фактически произошло (UTC, для cross-timezone consistency).</summary>
        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    }
}
