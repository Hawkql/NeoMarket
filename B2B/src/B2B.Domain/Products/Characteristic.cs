// B2B.Domain/Products/Characteristic.cs
namespace B2B.Domain.Products;

public class Characteristic
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Value { get; private set; } = null!;

    private Characteristic() { }

    public static Characteristic Create(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required", nameof(value));

        return new Characteristic
        {
            Id = Guid.NewGuid(),
            Name = name,
            Value = value
        };
    }
}