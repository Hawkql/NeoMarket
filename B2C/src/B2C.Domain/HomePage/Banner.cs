using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.HomePage
{
    public sealed class Banner : AggregateRoot<Guid>, IAuditableEntity
    {
        public string Title { get; private set; } = null!;
        public string ImageUrl { get; private set; } = null!;
        public string? LinkUrl { get; private set; }
        public int Priority { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime? StartsAt { get; private set; }
        public DateTime? EndsAt { get; private set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Banner() { }

        private Banner(Guid id, string title, string imageUrl) : base(id)
        {
            Title = title;
            ImageUrl = imageUrl;
            IsActive = true;
        }

        public static Banner Create(string title, string imageUrl, string? linkUrl,
            int priority, DateTime? startsAt, DateTime? endsAt)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Title is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new DomainException("ImageUrl is required", "INVALID_REQUEST");
            ValidateSchedule(startsAt, endsAt);

            return new Banner(Guid.NewGuid(), title, imageUrl)
            {
                LinkUrl = linkUrl,
                Priority = priority,
                StartsAt = startsAt,
                EndsAt = endsAt,
            };
        }

        public void Update(string title, string imageUrl, string? linkUrl,
            int priority, DateTime? startsAt, DateTime? endsAt)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Title is required", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new DomainException("ImageUrl is required", "INVALID_REQUEST");
            ValidateSchedule(startsAt, endsAt);

            Title = title;
            ImageUrl = imageUrl;
            LinkUrl = linkUrl;
            Priority = priority;
            StartsAt = startsAt;
            EndsAt = endsAt;
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        /// <summary>
        /// Виден ли баннер в указанный момент: активен + в расписании.
        /// Используется при выдаче GET /home/banners (фильтрация на уровне репозитория,
        /// но логика "что значит виден" принадлежит домену).
        /// </summary>
        public bool IsVisibleAt(DateTime now)
        {
            if (!IsActive) return false;
            if (StartsAt is not null && now < StartsAt) return false;
            if (EndsAt is not null && now > EndsAt) return false;
            return true;
        }

        private static void ValidateSchedule(DateTime? startsAt, DateTime? endsAt)
        {
            if (startsAt is not null && endsAt is not null && startsAt >= endsAt)
                throw new DomainException("StartsAt must be before EndsAt", "INVALID_REQUEST");
        }
    }
}
