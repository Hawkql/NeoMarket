using B2C.Application.Common.Abstractions;

namespace B2C.Api.Auth
{
    public sealed class SessionContext : ISessionContext
    {
        private const string HeaderName = "X-Session-Id";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? SessionId
        {
            get
            {
                var ctx = _httpContextAccessor.HttpContext;
                if (ctx is null) return null;

                if (ctx.Request.Headers.TryGetValue(HeaderName, out var value))
                {
                    var sessionId = value.ToString();
                    return string.IsNullOrWhiteSpace(sessionId) ? null : sessionId;
                }

                return null;
            }
        }
    }
}
