namespace B2C.Api.Contracts
{
    public sealed record RegisterRequest(
        string Email,
        string Password,
        string? FirstName,
        string? LastName,
        string? Phone);

    public sealed record LoginRequest(
        string Email,
        string Password);
    // SessionId для merge гостевой корзины берётся из заголовка X-Session-Id,
    // не из тела — контроллер прокинет его в LoginCommand.

    public sealed record RefreshRequest(string RefreshToken);

    public sealed record LogoutRequest(string RefreshToken);

    public sealed record ChangePasswordRequest(string NewPassword);

    public sealed record UpdateProfileRequest(
        string? FirstName,
        string? LastName,
        string? Phone);
}
