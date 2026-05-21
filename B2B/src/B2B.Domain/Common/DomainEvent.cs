using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Domain.Common
{
    public abstract record DomainEvent(DateTime OccurredOn) : INotification
    {
        /// <summary>Уникальный ID события (для дедупликации, логов, корреляции).</summary>
        public Guid EventId { get; init; } = Guid.NewGuid();

        protected DomainEvent() : this(DateTime.UtcNow) { }
        /// <summary>Когда событие произошло (UTC).</summary>
        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    }
}
