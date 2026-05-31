using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace B2B.Api.Authentication
{
    public sealed class ServiceKeyAuthenticationHandler
        : AuthenticationHandler<ServiceKeyAuthenticationOptions>
    {
        public ServiceKeyAuthenticationHandler(
            IOptionsMonitor<ServiceKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(
                    ServiceKeyAuthenticationOptions.HeaderName, out var provided))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var key = provided.ToString();
            if (string.IsNullOrEmpty(Options.ExpectedKey) ||
                !CryptographicEquals(key, Options.ExpectedKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid service key"));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "internal-service"),
                new Claim("scope", "service")
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        /// <summary>
        /// 401: запрос без валидного X-Service-Key. Возвращаем плоский {code, message}
        /// согласно контракту (иначе ответ был бы пустым и обходил ExceptionHandlingMiddleware).
        /// </summary>
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/json; charset=utf-8";
            var body = JsonSerializer.Serialize(new
            {
                code = "UNAUTHORIZED",
                message = "Missing or invalid X-Service-Key header"
            });
            await Response.WriteAsync(body, Encoding.UTF8);
        }

        /// <summary>
        /// 403: ключ валиден, но политика не пускает на ресурс. Тот же контракт.
        /// </summary>
        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            Response.ContentType = "application/json; charset=utf-8";
            var body = JsonSerializer.Serialize(new
            {
                code = "FORBIDDEN",
                message = "Service key does not grant access to this resource"
            });
            await Response.WriteAsync(body, Encoding.UTF8);
        }

        // Сравнение в постоянном времени — защита от timing attack на ключ
        private static bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length)
                return false;
            var result = 0;
            for (var i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];
            return result == 0;
        }
    }
}