using Microsoft.AspNetCore.Authentication;

namespace B2B.Api.Authentication
{
    public sealed class ServiceKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string SchemeName = "ServiceKey";
        public const string HeaderName = "X-Service-Key";

        /// <summary>Ожидаемое значение ключа (из конфигурации).</summary>
        public string ExpectedKey { get; set; } = null!;
    }
}
