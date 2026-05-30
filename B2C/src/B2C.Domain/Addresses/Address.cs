using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Addresses
{

    public sealed class Address : AggregateRoot<Guid>, IAuditableEntity
    {
        public Guid BuyerId { get; private set; }
        public string Country { get; private set; } = null!;
        public string City { get; private set; } = null!;
        public string Street { get; private set; } = null!;
        public string? House { get; private set; }
        public string? Apartment { get; private set; }
        public string? PostalCode { get; private set; }
        public bool IsDefault { get; private set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Address() { }

        private Address(Guid id, Guid buyerId, string country, string city, string street) : base(id)
        {
            BuyerId = buyerId;
            Country = country;
            City = city;
            Street = street;
        }

        public static Address Create(Guid buyerId, string country, string city, string street,
            string? house = null, string? apartment = null, string? postalCode = null, bool isDefault = false)
        {
            if (buyerId == Guid.Empty)
                throw new DomainException("BuyerId is required", "INVALID_REQUEST");
            ValidateRequired(country, nameof(country));
            ValidateRequired(city, nameof(city));
            ValidateRequired(street, nameof(street));

            return new Address(Guid.NewGuid(), buyerId, country, city, street)
            {
                House = house,
                Apartment = apartment,
                PostalCode = postalCode,
                IsDefault = isDefault,
            };
        }

        public void Update(string country, string city, string street, string? house, string? apartment, string? postalCode)
        {
            ValidateRequired(country, nameof(country));
            ValidateRequired(city, nameof(city));
            ValidateRequired(street, nameof(street));

            Country = country;
            City = city;
            Street = street;
            House = house;
            Apartment = apartment;
            PostalCode = postalCode;
        }

        public void MarkAsDefault() => IsDefault = true;
        public void UnmarkDefault() => IsDefault = false;

        /// <summary>
        /// Сериализация адреса в человекочитаемую строку — для копирования в Order.DeliveryAddress
        /// (исторический снимок, см. Order/DeliveryAddress).
        /// </summary>
        public string ToSnapshot()
        {
            var parts = new[] { Country, City, Street, House, Apartment, PostalCode };
            return string.Join(", ", Array.FindAll(parts, p => !string.IsNullOrWhiteSpace(p)));
        }

        private static void ValidateRequired(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException($"{field} is required", "INVALID_REQUEST");
        }
    }
}
