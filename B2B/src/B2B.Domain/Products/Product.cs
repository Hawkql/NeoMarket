using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;
using B2B.Domain.Products.Events;

namespace B2B.Domain.Products
{
    public class Product : AggregateRoot<Guid>
    {
        private readonly List<Sku> _skus = new();
        private readonly List<ProductImage> _images = new();
        private readonly List<Characteristic> _characteristics = new();
        public IReadOnlyList<Sku> Skus=> _skus.AsReadOnly();
        public IReadOnlyList<ProductImage> Images=> _images.AsReadOnly();
        public IReadOnlyList<Characteristic> Characteristics=> _characteristics.AsReadOnly();

        

        public string Title { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public string Slug { get; private set; } = null!;
        public Guid CategoryId{ get; private set; }
        public Guid SelleryId {  get; private set; }
        public ProductStatus Status { get; private set; }

        private Product() { }

        public static Product Create(
            string title,
            string description,
            Guid categoryId,
            Guid selleryId
            )
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Product title is required");
            if(title.Length >= 255)
                throw new DomainException("Product title is too long");

            var product = new Product()
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description?? string.Empty,
                Slug =GenerateSlug(title),
                CategoryId = categoryId,
                SelleryId = selleryId,
                Status = ProductStatus.Created,

            };
            product.AddDomainEvent(new ProductCreatedEvent(product.Id, selleryId));
            return product;
        }

        public void Update(string title,string description)
        {
            if(Status==ProductStatus.Blocked)
                throw new DomainException("Cannot update blocked product");
            Title = title;
            Description = description;
            Status = ProductStatus.Created;
            AddDomainEvent(new ProductCreatedEvent(Id, SelleryId));
        }
        public void AddSku(string name, decimal price, IEnumerable<Characteristic> characteristics)
        {
            if (price < 0) throw new DomainException("Price must be positive");

            // Защита инварианта: нельзя добавить SKU с теми же характеристиками
            var newCharSet = characteristics.Select(c => $"{c.Name}={c.Value}").OrderBy(x => x);
            foreach (var existingSku in _skus)
            {
                var existingCharSet = existingSku._characteristics
                    .Select(c => $"{c.Name}={c.Value}").OrderBy(x => x);
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
            if(Status!=ProductStatus.OnModeration)
                throw new DomainException($"Cannot approve product in status {Status}");
            Status =ProductStatus.Moderation;
        }
        public void Block()
        {
            Status = ProductStatus.Blocked;
        }

        private static string GenerateSlug(string title )=>title.ToLowerInvariant().Replace(" ","-");
    }
    
}
