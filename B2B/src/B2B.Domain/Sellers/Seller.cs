using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Sellers
{
    public sealed class Seller : AggregateRoot<Guid>, IAuditableEntity
    {
        public string Email { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;
        public string FirstName { get; private set; } = null!;
        public string LastName { get; private set; } = null!;
        public string? MiddleName { get; private set; }
        public string CompanyName { get; private set; } = null!;
        public string Inn { get; private set; } = null!;
        public string? Phone { get; private set; }
        public bool Deleted { get; private set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Seller() { }

        private Seller(
            Guid id, string email, string passwordHash,
            string firstName, string lastName, string? middleName,
            string companyName, string inn, string? phone) : base(id)
        {
            Email = email;
            PasswordHash = passwordHash;
            FirstName = firstName;
            LastName = lastName;
            MiddleName = middleName;
            CompanyName = companyName;
            Inn = inn;
            Phone = phone;
            Deleted = false;
        }

        /// <summary>
        /// Фабрика. passwordHash — уже захэшированный пароль (хэширование —
        /// в Application через IPasswordHasher, Domain не знает про BCrypt).
        /// </summary>
        public static Seller Create(
            string email, string passwordHash,
            string firstName, string lastName, string? middleName,
            string companyName, string inn, string? phone)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("email is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("password is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainException("first_name is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(lastName))
                throw new DomainException("last_name is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(companyName))
                throw new DomainException("company_name is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(inn) || inn.Length is < 10 or > 12)
                throw new DomainException("inn must be 10-12 chars", "INVALID_REQUEST");

            return new Seller(
                Guid.NewGuid(), email.Trim().ToLowerInvariant(), passwordHash,
                firstName, lastName, middleName, companyName, inn, phone);
        }

        /// <summary>PATCH профиля. null = не менять.</summary>
        public void UpdateProfile(
            string? firstName, string? lastName, string? middleName,
            string? companyName, string? phone)
        {
            if (Deleted)
                throw new DomainException("Account is deleted", "FORBIDDEN");

            if (firstName is not null) FirstName = firstName;
            if (lastName is not null) LastName = lastName;
            if (middleName is not null) MiddleName = middleName;
            if (companyName is not null) CompanyName = companyName;
            if (phone is not null) Phone = phone;
        }

        public void MarkAsDeleted()
        {
            if (Deleted)
                throw new DomainException("Account already deleted", "INVALID_REQUEST");
            Deleted = true;
        }
    }
}
