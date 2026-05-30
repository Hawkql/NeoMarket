using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Buyers
{
    public sealed class Buyer : AggregateRoot<Guid>, IAuditableEntity
    {
        public string Email { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string? Phone { get; private set; }
        public bool Deleted { get; private set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Buyer() { }

        private Buyer(Guid id, string email, string passwordHash,
            string? firstName, string? lastName, string? phone) : base(id)
        {
            Email = email;
            PasswordHash = passwordHash;
            FirstName = firstName;
            LastName = lastName;
            Phone = phone;
            Deleted = false;
        }

        /// <summary>
        /// Фабрика. passwordHash — уже захэшированный пароль (хэширование —
        /// в Application через IPasswordHasher, Domain не знает про BCrypt).
        /// </summary>
        public static Buyer Register(
            string email, string passwordHash,
            string? firstName = null, string? lastName = null, string? phone = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("email is required", "INVALID_REQUEST");
            if (email.Length > 255 || !email.Contains('@'))
                throw new DomainException("email is invalid", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("password is required", "INVALID_REQUEST");

            return new Buyer(
                Guid.NewGuid(), email.Trim().ToLowerInvariant(), passwordHash,
                firstName, lastName, phone);
        }

        /// <summary>PATCH профиля. null = не менять.</summary>
        public void UpdateProfile(string? firstName, string? lastName, string? phone)
        {
            if (Deleted)
                throw new DomainException("Account is deleted", "FORBIDDEN");

            if (firstName is not null) FirstName = firstName;
            if (lastName is not null) LastName = lastName;
            if (phone is not null) Phone = phone;
        }

        public void ChangePassword(string newPasswordHash)
        {
            if (Deleted)
                throw new DomainException("Account is deleted", "FORBIDDEN");
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new DomainException("password is required", "INVALID_REQUEST");
            PasswordHash = newPasswordHash;
        }

        public void MarkAsDeleted()
        {
            if (Deleted)
                throw new DomainException("Account already deleted", "INVALID_REQUEST");

            Deleted = true;
            // PII обнуляем — soft delete, но без личных данных.
            FirstName = null;
            LastName = null;
            Phone = null;
        }
    }
}
