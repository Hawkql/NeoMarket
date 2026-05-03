using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;
using B2B.Domain.Products.Events;

namespace B2B.Domain.Products
{
    // B2B.Domain/Products/Product.cs
    public class Product : AggregateRoot<Guid>
    {

        private readonly List<ProductCharacteristic> _characteristics = new();
        private readonly List<FieldReport> _fieldReports = new();

        // identity
        public Guid SellerId { get; private set; }
        public Guid CategoryId { get; private set; }

        // content
        public string Title { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public IReadOnlyList<ProductCharacteristic> Characteristics => _characteristics.AsReadOnly();

        //lifecycle
        public ProductStatus Status { get; private set; }
        public bool Deleded { get; private set; }

        public bool Blocked => Status is ProductStatus.Blocked or ProductStatus.HardBlocked;

        public BlockingReason? BlockingReason { get; private set; }
        public IReadOnlyList<FieldReport> fieldReports => _fieldReports.AsReadOnly();

        /// <summary>
        /// Номер текущего раунда модерации. 0 — товар никогда не был на модерации.
        /// Инкрементируется при каждом отправлении на модерацию.
        /// </summary>
        public int ModerationRound { get; private set; }

        //audit
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private Product() { }
        private Product(Guid id, Guid sellerId, Guid categoryId,
        string title, string description)
        {

            Title = title;
            Description = description ?? string.Empty;
            CategoryId = categoryId;
            SellerId = sellerId;
            Status = ProductStatus.Created;
            Deleded = false;
            ModerationRound = 0;
        }

        public static Product Create(
            Guid categoryId,
            Guid sellerId,
            string title,
            string description,
            IEnumerable<ProductCharacteristic>? characteristics = null
            )
        {
            if (sellerId == Guid.Empty)
                throw new DomainException("SellerId is required", "INVALID_REQUEST");
            if (categoryId == Guid.Empty)
                throw new DomainException("CategoryId is required", "INVALID_REQUEST");
            ValidateContent(title, description);

            var product = new Product(Guid.NewGuid(),sellerId, categoryId, title, description);
            if(characteristics is not null)
                product._characteristics.AddRange(characteristics);
            
            product.AddDomainEvent(new ProductCreatedEvent(product.Id, sellerId));
            return product;
        }
        /// <summary>
        /// ///////////////////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <exception cref="DomainException"></exception>
        public void Update(string title, string description)
        {
            if (Status == ProductStatus.Blocked)
                throw new DomainException("Cannot update blocked product");
            Title = title;
            Description = description;
            Status = ProductStatus.Created;
            AddDomainEvent(new ProductUpdatedEvent(Id, SellerId));
        }

        public void AddCharacteristic(string name, string value)
        {
            var characteristic = ProductCharacteristic.Create(name, value);
            _characteristics.Add(characteristic);
        }

        public void AddSku(string name, decimal price, IEnumerable<ProductCharacteristic> characteristics)
        {
            if (price <= 0) throw new DomainException("Price must be positive");

            var newCharSet = characteristics.Select(c => $"{c.Name}={c.Value}").OrderBy(x => x).ToList();
            foreach (var existingSku in _skus)
            {
                var existingCharSet = existingSku.Characteristics
                    .Select(c => $"{c.Name}={c.Value}").OrderBy(x => x).ToList();
                if (existingCharSet.SequenceEqual(newCharSet))
                    throw new DomainException("SKU with same characteristics already exists");
            }
            var sku = Sku.Create(Id, name, price, characteristics);
            _skus.Add(sku);
        }

        public void IncreaseSkuQuantity(Guid skuId, int amount)
        {
            var sku = _skus.FirstOrDefault(s => s.Id == skuId)
                ?? throw new DomainException("SKU not found in product");
            sku.IncreaseQuantity(amount);
        }

        public void Approve()
        {
            if (Status != ProductStatus.OnModeration)
                throw new DomainException($"Cannot approve product in status {Status}");
            Status = ProductStatus.Moderated;
        }

        public void Block() => Status = ProductStatus.Blocked;
        private static void ValidateContent(string title, string description)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("title is required", "INVALID_REQUEST");
            if (title.Length > 255)
                throw new DomainException("title must be 1-255 characters", "INVALID_REQUEST");
            if (string.IsNullOrWhiteSpace(description))
                throw new DomainException("description is required", "INVALID_REQUEST");
            if (description.Length > 5000)
                throw new DomainException("description must be 1-5000 characters", "INVALID_REQUEST");
        }
        private static string GenerateSlug(string title) =>
            title.ToLowerInvariant().Replace(" ", "-");
    }
}
