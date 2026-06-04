using B2C.Domain.Common;

namespace B2C.Domain.Orders
{
    /// <summary>
    /// Snapshot адреса доставки на момент создания заказа.
    /// Value Object: копия полей Buyer.Address в момент checkout —
    /// если адрес покупателя потом удалится, заказ показывает прежний.
    /// </summary>
    public sealed class OrderAddress
    {
        public string Country { get; private set; } = null!;
        public string City { get; private set; } = null!;
        public string Street { get; private set; } = null!;
        public string? House { get; private set; }
        public string? Apartment { get; private set; }
        public string? PostalCode { get; private set; }

        /// <summary>
        /// Ссылка на исходный Address.Id (для трейсинга — «из какого адреса покупателя
        /// взят snapshot»). Сам адрес покупателя может быть удалён — snapshot остаётся.
        /// </summary>
        public Guid? OriginalAddressId { get; private set; }

        // EF requires parameterless ctor for owned types.
        private OrderAddress() { }

        public OrderAddress(
            Guid? originalAddressId,
            string country, string city, string street,
            string? house = null, string? apartment = null, string? postalCode = null)
        {
            if (string.IsNullOrWhiteSpace(country))
                throw new DomainException("address.country is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(city))
                throw new DomainException("address.city is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(street))
                throw new DomainException("address.street is required", "INVALID_REQUEST");

            OriginalAddressId = originalAddressId;
            Country = country;
            City = city;
            Street = street;
            House = house;
            Apartment = apartment;
            PostalCode = postalCode;
        }
    }
}