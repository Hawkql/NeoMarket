using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Агрегат «Товар» — полная карточка с характеристиками и SKU.
    /// Соответствует GET /api/v1/products/{id}
    /// </summary>
    public sealed class Product
    {
        public Guid Id { get; private set; }
        public string Slug { get; private set; } = string.Empty;
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public ProductStatus Status { get; private set; }
        public Guid? CategoryId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<ProductImage> _images = [];
        private readonly List<Characteristic> _characteristics = [];
        private readonly List<Sku> _skus = [];

        public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
        public IReadOnlyCollection<Characteristic> Characteristics => _characteristics.AsReadOnly();
        public IReadOnlyCollection<Sku> Skus => _skus.AsReadOnly();

        // EF Core constructor
        private Product() { }

        public static Product Create(
            Guid id,
            string slug,
            string title,
            string description,
            ProductStatus status,
            Guid? categoryId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(slug);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            return new Product
            {
                Id = id,
                Slug = slug,
                Title = title,
                Description = description,
                Status = status,
                CategoryId = categoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>Доступен ли товар на публичной витрине</summary>
        public bool IsPubliclyVisible => Status == ProductStatus.Moderated;

        public decimal MinPrice => _skus.Count > 0 ? _skus.Min(s => s.Price) : 0m;
        public bool HasStock => _skus.Any(s => s.IsInStock);

        public void AddImage(ProductImage image) => _images.Add(image);
        public void AddCharacteristic(Characteristic characteristic) => _characteristics.Add(characteristic);
        public void AddSku(Sku sku) => _skus.Add(sku);

    }


    public sealed class ProductImage
    {
        public Guid Id { get; private set; }
        public string Url { get; private set; } = string.Empty;
        public int Order { get; private set; }

        // Nullable — изображение может принадлежать либо товару, либо SKU
        public Guid? ProductId { get; private set; }
        public Guid? SkuId { get; private set; }

        private ProductImage() { }

        public static ProductImage ForProduct(string url, int order, Guid productId) =>
            new() { Id = Guid.NewGuid(), Url = url, Order = order, ProductId = productId };

        public static ProductImage ForSku(string url, int order, Guid skuId) =>
            new() { Id = Guid.NewGuid(), Url = url, Order = order, SkuId = skuId };
    }

    public sealed class Characteristic
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Value { get; private set; } = string.Empty;

        public Guid? ProductId { get; private set; }
        public Guid? SkuId { get; private set; }

        private Characteristic() { }

        public static Characteristic ForProduct(string name, string value, Guid productId) =>
            new() { Id = Guid.NewGuid(), Name = name, Value = value, ProductId = productId };

        public static Characteristic ForSku(string name, string value, Guid skuId) =>
            new() { Id = Guid.NewGuid(), Name = name, Value = value, SkuId = skuId };
    }
}
