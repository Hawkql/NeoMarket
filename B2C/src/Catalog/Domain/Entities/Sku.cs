using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{

    /// <summary>
    /// SKU — конкретный вариант товара (цвет + объём памяти и т.д.)
    /// </summary>
    public sealed class Sku
    {
        public Guid Id { get; private set; }
        public Guid ProductId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int Quantity { get; private set; }

        private readonly List<Characteristic> _characteristics = [];
        private readonly List<ProductImage> _images = [];

        public IReadOnlyCollection<Characteristic> Characteristics => _characteristics.AsReadOnly();
        public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

        // Навигационное свойство EF Core
        public Product? Product { get; private set; }

        private Sku() { }

        public static Sku Create(Guid id, Guid productId, string name, decimal price, int quantity)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");

            return new Sku()
            {
                Id = id,
                ProductId = productId,
                Name = name,
                Price = price,
                Quantity = quantity
            };
        }

        public bool IsInStock => Quantity > 0;

        public void AddCharacteristic(Characteristic c) => _characteristics.Add(c);
        public void AddImage(ProductImage img) => _images.Add(img);
    }
}
