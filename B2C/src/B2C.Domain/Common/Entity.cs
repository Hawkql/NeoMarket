using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Common
{
    public abstract class Entity<TId> : IEquatable<Entity<TId>> where TId : struct
    {
        public TId Id { get; protected set; } = default!;

        // Параметрless-конструктор нужен EF Core для материализации из БД.
        // Protected, чтобы извне нельзя было создать "пустую" сущность.
        protected Entity() { }

        protected Entity(TId id) => Id = id;

        public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

        public bool Equals(Entity<TId>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            // КРИТИЧНО: сравнение по Id И типу.
            // Без проверки GetType() Buyer с Id=X и Order с Id=X считались бы равными,
            // что ломает HashSet и навигационные свойства EF Core.
            return Id.Equals(other.Id) && GetType() == other.GetType();
        }

        public override int GetHashCode() => HashCode.Combine(GetType(), Id);

        public static bool operator ==(Entity<TId>? a, Entity<TId>? b) => Equals(a, b);
        public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !Equals(a, b);
    }
}
