namespace B2B.Api.Contracts
{
    public sealed record UpdateSellerRequest(
        string? FirstName,
        string? LastName,
        string? MiddleName,
        string? CompanyName,
        string? Phone);
}
