using System.Security.Claims;
using B2B.Application.Common.Abstractions;

namespace B2B.Api.Services
{

    /// <summary>
    /// Извлекает seller_id и role из JWT claims.
    /// IDOR prevention: seller_id берётся ТОЛЬКО из токена.
    /// </summary>
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        private Guid ResolveUserId()
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id)
                ? id
                : throw new UnauthorizedAccessException("Invalid or missing sub claim");
        }

        public Guid UserId => ResolveUserId();
        public Guid SellerId => ResolveUserId();   // алиас, семантика "seller"

        public string Role =>
            User?.FindFirstValue(ClaimTypes.Role)
            ?? User?.FindFirstValue("role")
            ?? string.Empty;
    }
}
