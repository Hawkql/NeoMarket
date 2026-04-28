using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Агрегат «Категория».
    /// B2C получает плоский список из B2B и строит дерево самостоятельно.
    /// </summary>
    public sealed class Category
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Slug { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public Guid? ParentId { get; private set; }
        public string? ImageUrl { get; private set; }
        public bool IsActive { get; private set; }
        public int? SortOrder { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // SEO
        public string SeoTitle { get; private set; } = string.Empty;
        public string SeoDescription { get; private set; } = string.Empty;
        public string SeoKeywords { get; private set; } = string.Empty; // JSON array

        // Open Graph
        public string? OgTitle { get; private set; }
        public string? OgDescription { get; private set; }
        public string? OgImage { get; private set; }
        public string? TwitterCard { get; private set; }


        public string SeoKeywordsJson { get; private set; } = "[]";

        // Навигация EF Core
        public Category? Parent { get; private set; }
        private readonly List<Category> _children = [];
        public IReadOnlyCollection<Category> Children => _children.AsReadOnly();

        // Фильтры привязаны к категории
        private readonly List<CategoryFilter> _filters = [];
        public IReadOnlyCollection<CategoryFilter> Filters => _filters.AsReadOnly();

        private Category() { }

        public static Category Create(Guid id, string name, string slug, Guid? parentId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(slug);

            return new Category
            {
                Id = id,
                Name = name,
                Slug = slug,
                ParentId = parentId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public void AddChild(Category child) => _children.Add(child);
        public void AddFilter(CategoryFilter filter) => _filters.Add(filter);
    }

    /// <summary>
    /// Фильтр (характеристика), доступный в категории.
    /// Соответствует GET /api/v1/categories/{id}/filters
    /// </summary>
    public sealed class CategoryFilter
    {
        public Guid Id { get; private set; }
        public Guid CategoryId { get; private set; }
        public string Slug { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string FilterType { get; private set; } = string.Empty; // list | range | switch
        public string? ValuesJson { get; private set; }  // JSON array для type=list
        public decimal? MinValue { get; private set; }
        public decimal? MaxValue { get; private set; }

        public Category? Category { get; private set; }

        private CategoryFilter() { }

        public static CategoryFilter Create(
            Guid categoryId,
            string slug,
            string name,
            string filterType,
            string? valuesJson = null,
            decimal? min = null,
            decimal? max = null) =>
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                Slug = slug,
                Name = name,
                FilterType = filterType,
                ValuesJson = valuesJson,
                MinValue = min,
                MaxValue = max
            };
    }
}
