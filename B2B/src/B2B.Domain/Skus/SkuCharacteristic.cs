using B2B.Domain.Common;

namespace B2B.Domain.Skus
{
    /// <summary>
    /// Value Object: характеристика SKU (цвет, размер, объём памяти).
    /// </summary>
    public sealed class SkuCharacteristic
    {
        public string Name { get; private set; } = null!;
        public string Value { get; private set; } = null!;
        public SkuCharacteristic() { }

        public SkuCharacteristic(string name,string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("characteristic name is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("characteristic value is required", "INVALID_REQUEST");
            if (name.Length > 100)
                throw new DomainException("characteristic name too long", "INVALID_REQUEST");
            if (value.Length > 500)
                throw new DomainException("characteristic value too long", "INVALID_REQUEST");
            Name = name;
            Value = value;
        }

    }
}