using B2B.Domain.Common;

namespace B2B.Domain.Skus
{
    /// <summary>
    /// Характеристика SKU (цвет, размер, объём памяти). Имеет Id для API-контракта
    /// (CharacteristicResponse.id). Часть агрегата Sku — без своего репозитория.
    /// </summary>
    public sealed class SkuCharacteristic
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = null!;
        public string Value { get; private set; } = null!;

        private SkuCharacteristic() { }

        public SkuCharacteristic(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("characteristic name is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("characteristic value is required", "INVALID_REQUEST");
            if (name.Length > 100)
                throw new DomainException("characteristic name too long", "INVALID_REQUEST");
            if (value.Length > 500)
                throw new DomainException("characteristic value too long", "INVALID_REQUEST");

            Id = Guid.NewGuid();
            Name = name;
            Value = value;
        }
    }
}