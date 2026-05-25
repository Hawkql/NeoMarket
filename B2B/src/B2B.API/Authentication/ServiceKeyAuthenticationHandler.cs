using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
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

            // 401 сформируется на этапе Challenge, если эндпоинт требует ServiceOnly.
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

            // Успех — создаём principal с ролью "service"
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
