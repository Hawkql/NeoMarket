using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Common
{
    public abstract class AggregateRoot<TId> : Entity<TId> where TId : struct
    {
        private readonly List<DomainEvent> _domainEvents = new();

        /// <summary>
        /// События, поднятые этим агрегатом в текущей единице работы.
        /// Readonly наружу — извне нельзя дописать событие "в обход" агрегата.
        /// </summary>
        public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected AggregateRoot() { }

        protected AggregateRoot(TId id) : base(id) { }

        /// <summary>
        /// Поднять доменное событие. Protected: события поднимает только сам агрегат
        /// изнутри своих методов (Tell, Don't Ask) — внешний код не может "добавить" событие.
        /// </summary>
        protected void RaiseDomainEvent(DomainEvent domainEvent)
        {
            if (domainEvent is null)
                throw new ArgumentNullException(nameof(domainEvent));

            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Очистить список событий. Вызывается инфраструктурой ПОСЛЕ того,
        /// как события переложены в Outbox — иначе при повторном SaveChanges
        /// одно и то же событие попадёт в Outbox дважды.
        /// </summary>
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
