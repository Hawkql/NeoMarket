using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;
using B2B.Domain.Products;
using B2B.Domain.Skus.Events;

namespace B2B.Domain.Skus
{
    public sealed class Sku : AggregateRoot<Guid>, IAuditableEntity
    {
        private readonly List<SkuCharacteristic> _characteristics = new();
        public Guid ProductId { get; private set; }
        public string Name { get; private set; } = null!;
        public int Price { get; private set; }
        public int? CostPrice { get; private set; }
        public int Discount { get; private set; }
        public string? Article { get; private set; }
        public string ImageUrl { get; private set; } = string.Empty;
        public int ActiveQuantity { get; private set; }
        public int ReservedQuantity { get; private set; }
        public bool Deleted { get; private set; }
        public IReadOnlyCollection<SkuCharacteristic> Characteristics => _characteristics.AsReadOnly();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Sku() { }

        private Sku(Guid id, Guid productId, string name, int price,
            int? costPrice, int discount, string? article, string imageUrl) : base(id)
        {
            ProductId = productId;
            Name = name;
            Price = price;
            CostPrice = costPrice;
            Discount = discount;
            Article = article;
            ImageUrl = imageUrl;
            ActiveQuantity = 0;
            ReservedQuantity = 0;
        }

        public static Sku Create(
            Guid productId,
            string name,
            int price,
            int? costPrice,
            int discount,
            string? article,
            string imageUrl,
            IEnumerable<SkuCharacteristic>? characteristics = null)
        {
            if (productId == Guid.Empty)
                throw new DomainException("ProductId is required", "INVALID_REQUEST");

            ValidateMutableFields(name, price, costPrice, discount);

            var sku = new Sku(Guid.NewGuid(), productId, name, price,
                costPrice, discount, article, imageUrl ?? string.Empty);

            if (characteristics is not null)
                sku._characteristics.AddRange(characteristics);

            sku.RaiseDomainEvent(new SkuCreatedEvent(sku.Id, sku.ProductId));
            return sku;
        }

        public void Update(
            string name,
            int price,
            int? costPrice,
            int discount,
            string? article,
            IEnumerable<SkuCharacteristic>? characteristics = null)
        {
            if (Deleted)
                throw new DomainException("Cannot update deleted SKU", "FORBIDDEN");

            ValidateMutableFields(name, price, costPrice, discount);

            Name = name;
            Price = price;
            CostPrice = costPrice;
            Discount = discount;
            Article = article;

            if (characteristics is not null)
            {
                _characteristics.Clear();
                _characteristics.AddRange(characteristics);
            }

            RaiseDomainEvent(new SkuUpdatedEvent(Id, ProductId));
        }

        public void SetCoverImage(string imageUrl)
        {
            ImageUrl = imageUrl ?? string.Empty;
        }

        public void IncreaseStock(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("quantity must be positive", "INVALID_REQUEST");
            ActiveQuantity += quantity;
        }

        public void Reserve(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("quantity must be positive", "INVALID_REQUEST");
            if (ActiveQuantity < quantity)
                throw new DomainException(
                    $"Insufficient stock: have {ActiveQuantity}, requested {quantity}",
                    "INSUFFICIENT_STOCK");
            ActiveQuantity -= quantity;
            ReservedQuantity += quantity;
            if (ActiveQuantity == 0)
                RaiseDomainEvent(new SkuOutOfStockEvent(Id, ProductId));
        }

        public void UnReserve(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("quantity must be positive", "INVALID_REQUEST");
            if (ReservedQuantity < quantity)
                throw new DomainException(
                    $"Cannot unreserve {quantity}: only {ReservedQuantity} reserved",
                    "INVALID_REQUEST");
            ActiveQuantity += quantity;
            ReservedQuantity -= quantity;
        }

        public void Fulfill(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("quantity must be positive", "INVALID_REQUEST");
            if (ReservedQuantity < quantity)
                throw new DomainException(
                    $"Cannot fulfill {quantity}: only {ReservedQuantity} reserved",
                    "INVALID_REQUEST");
            ReservedQuantity -= quantity;
        }

        public void MarkAsDeleted()
        {
            if (Deleted)
                throw new DomainException("SKU already deleted", "INVALID_REQUEST");
            if (ReservedQuantity > 0)
                throw new DomainException(
                    "Cannot delete SKU with active reserves", "CONFLICT");
            Deleted = true;
            RaiseDomainEvent(new SkuDeletedEvent(Id, ProductId));
        }
        /// <summary>
        /// При удалении SKU из активной витрины (товар MODERATED, был доступный остаток)
        /// — уведомить B2C, что позиция выбыла. US-12.
        /// </summary>
        public void RaiseOutOfStockOnRemoval()
        {
            if (ActiveQuantity > 0)
                RaiseDomainEvent(new SkuOutOfStockEvent(Id, ProductId));
        }
        private static void ValidateMutableFields(
            string name, int price, int? costPrice, int discount)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("name is required", "INVALID_REQUEST");
            if (name.Length > 255)
                throw new DomainException("name must be 1-255 characters", "INVALID_REQUEST");
            if (price < 0)
                throw new DomainException("price must be >= 0 (kopecks)", "INVALID_REQUEST");
            if (costPrice is < 0)
                throw new DomainException("cost_price must be >= 0 (kopecks)", "INVALID_REQUEST");
            if (discount < 0)
                throw new DomainException("discount must be >= 0", "INVALID_REQUEST");
            if (price > 0 && discount >= price)
                throw new DomainException("discount must be less than price", "INVALID_REQUEST");
        }
    }
}
