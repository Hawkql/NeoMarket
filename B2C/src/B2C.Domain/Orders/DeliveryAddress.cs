using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Orders
{
    public sealed class DeliveryAddress
    {
        public string Value { get; private set; } = null!;

        private DeliveryAddress() { }

        private DeliveryAddress(string value) => Value = value;

        public static DeliveryAddress Of(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new DomainException("delivery_address is required", "INVALID_REQUEST");
            if (raw.Length > 500)
                throw new DomainException("delivery_address too long", "INVALID_REQUEST");
            return new DeliveryAddress(raw.Trim());
        }

        public override string ToString() => Value;
    }
}
