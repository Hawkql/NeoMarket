using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Common;

namespace B2B.Domain.Products
{
    public class Sku : Entity<Guid>
{
    private readonly List<Characteristic> _characteristics = new();
    
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }
    public IReadOnlyList<Characteristic> Characteristics => _characteristics.AsReadOnly();

    private Sku() { }

    internal static Sku Create(
        Guid productId,
        string name,
        decimal price,
        IEnumerable<Characteristic> characteristics)
    {
        var sku = new Sku
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Name = name,
            Price = price,
            Quantity = 0
        };
        sku._characteristics.AddRange(characteristics);
        return sku;
    }

    internal void IncreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Increase amount must be positive");
        Quantity += amount;
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new DomainException("Price must be positive");
        Price = newPrice;
    }
}
}
