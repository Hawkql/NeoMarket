using System;
using System.Collections.Generic;
using System.Linq;
using B2C.Domain.Common;

namespace B2C.Domain.HomePage
{
    /// <summary>
    /// Подборка товаров для главной страницы (US-CART-05).
    /// 
    /// Хранит список ProductIds как массив — порядок значим, читаем подборку целиком.
    /// ACL: товары физически в B2B, мы храним только ID-шки. Обогащение делает Application
    /// при выдаче GetCollectionQuery.
    /// 
    /// Slug нормализуется в lowercase при создании/обновлении — обеспечивает SEO-friendly
    /// и предсказуемый поиск по уникальному индексу ux_collections_slug.
    /// </summary>
    public sealed class Collection : AggregateRoot<Guid>, IAuditableEntity
    {
        public string Slug { get; private set; } = null!;
        public string Title { get; private set; } = null!;
        public string? Description { get; private set; }
        public string? CoverImageUrl { get; private set; }
        public int Priority { get; private set; }
        public bool IsActive { get; private set; }

        // Backing field — EF маппит как PostgreSQL uuid[] через HasField("_productIds").
        private readonly List<Guid> _productIds = new();
        public IReadOnlyList<Guid> ProductIds => _productIds;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Collection() { }

        private Collection(Guid id, string slug, string title) : base(id)
        {
            Slug = slug;
            Title = title;
            IsActive = true;
        }

        public static Collection Create(
            string slug,
            string title,
            string? description,
            string? coverImageUrl,
            int priority,
            IEnumerable<Guid> productIds)
        {
            ValidateSlug(slug);
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Title is required", "INVALID_REQUEST");

            var collection = new Collection(Guid.NewGuid(), slug.Trim().ToLowerInvariant(), title)
            {
                Description = description,
                CoverImageUrl = coverImageUrl,
                Priority = priority,
            };

            if (productIds is not null)
                collection._productIds.AddRange(productIds.Where(id => id != Guid.Empty).Distinct());

            return collection;
        }

        public void Update(
            string slug, string title,
            string? description, string? coverImageUrl, int priority)
        {
            ValidateSlug(slug);
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Title is required", "INVALID_REQUEST");

            Slug = slug.Trim().ToLowerInvariant();
            Title = title;
            Description = description;
            CoverImageUrl = coverImageUrl;
            Priority = priority;
        }

        public void SetProducts(IEnumerable<Guid> productIds)
        {
            _productIds.Clear();
            if (productIds is not null)
                _productIds.AddRange(productIds.Where(id => id != Guid.Empty).Distinct());
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        private static void ValidateSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new DomainException("Slug is required", "INVALID_REQUEST");
            if (slug.Length > 100)
                throw new DomainException("Slug must be <= 100 chars", "INVALID_REQUEST");
        }
    }
}