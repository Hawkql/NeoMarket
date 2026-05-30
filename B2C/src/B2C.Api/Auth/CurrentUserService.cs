using System.Security.Claims;
using B2C.Application.Common.Abstractions;

namespace B2C.Api.Auth
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        public Guid BuyerId
        {
            get
            {
                // 'sub' claim по JWT-спеке = идентификатор покупателя.
                var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User?.FindFirstValue("sub");

                return Guid.TryParse(sub, out var id)
                    ? id
                    : throw new UnauthorizedAccessException("Invalid or missing sub claim");
            }
        }
    }
}
