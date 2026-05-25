namespace B2B.Api.Contracts
{
    public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? MiddleName,
    string CompanyName,
    string Inn,
    string? Phone);

    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);
}
