using Microsoft.AspNetCore.Authentication;

namespace B2C.Api.Auth
{
    public sealed class ServiceKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string SchemeName = "ServiceKey";
        public const string HeaderName = "X-Service-Key";

        public string ExpectedKey { get; set; } = string.Empty;
    }
}
