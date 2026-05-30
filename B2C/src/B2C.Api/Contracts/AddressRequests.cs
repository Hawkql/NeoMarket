namespace B2C.Api.Contracts
{
    public sealed record CreateAddressRequest(
        string Country,
        string City,
        string Street,
        string? House,
        string? Apartment,
        string? PostalCode,
        bool IsDefault);

    public sealed record UpdateAddressRequest(
        string Country,
        string City,
        string Street,
        string? House,
        string? Apartment,
        string? PostalCode);
}
