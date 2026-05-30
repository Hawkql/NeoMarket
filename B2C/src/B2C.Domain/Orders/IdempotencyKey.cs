using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Orders
{
    public sealed class IdempotencyKey : IEquatable<IdempotencyKey>
    {
        public Guid Value { get; private set; }

        private IdempotencyKey() { }

        private IdempotencyKey(Guid value) => Value = value;

        public static IdempotencyKey From(Guid value)
        {
            if (value == Guid.Empty)
                throw new DomainException("idempotency_key is required", "INVALID_REQUEST");
            return new IdempotencyKey(value);
        }

        public static IdempotencyKey Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new DomainException("idempotency_key is required", "INVALID_REQUEST");
            if (!Guid.TryParse(raw, out var guid) || guid == Guid.Empty)
                throw new DomainException("idempotency_key must be a UUID", "INVALID_REQUEST");
            return new IdempotencyKey(guid);
        }

        public override string ToString() => Value.ToString();

        public bool Equals(IdempotencyKey? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => Equals(obj as IdempotencyKey);
        public override int GetHashCode() => Value.GetHashCode();
    }
}
