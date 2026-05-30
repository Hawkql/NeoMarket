using System.Security.Claims;
using System.Text.Encodings.Web;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace B2C.Api.Auth
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
            // Нет заголовка → NoResult (не Fail), чтобы не перебивать другие схемы.
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
                new Claim(ClaimTypes.Name, "b2b-service"),
                new Claim("scope", "service"),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        /// <summary>Constant-time сравнение — защита от timing-атак.</summary>
        private static bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var result = 0;
            for (var i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];
            return result == 0;
        }
    }
}
