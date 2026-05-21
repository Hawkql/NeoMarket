    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Common
{
    public abstract class Entity<TId>:IEquatable<Entity<TId>> where TId : struct
    {
        public TId Id { get; protected set; } = default!;
        protected Entity() { }
        protected Entity(TId id)=>Id = id;

        public override bool Equals(object? obj) => Equals(obj as Entity<TId>);


        public bool Equals(Entity<TId>? other)
        {
            if (other is null) return false;
            if(ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id) && GetType()==other.GetType();
        }

        public override int GetHashCode() => HashCode.Combine(GetType(),Id);

        public static bool operator ==(Entity<TId>?a,Entity<TId>?b) =>Equals(a,b);
        public static bool operator !=(Entity<TId>?a,Entity<TId>?b) =>!Equals(a,b);
    }
}
