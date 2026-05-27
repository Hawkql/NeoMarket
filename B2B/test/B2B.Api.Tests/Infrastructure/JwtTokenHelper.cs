using System.IdentityModel.Tokens.Jwt;              
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace B2B.Api.Tests.Infrastructure
{
    public static class JwtTokenHelper
    {
        // Должен совпадать с Jwt:Secret в конфиге приложения (appsettings.json)
        private const string Secret =
            "fantkfotpkgfkwotmalfjtofkglrtjfmfltjaptsj";
        private const string Issuer = "neomarket-auth";
        private const string Audience = "neomarket";

        public static string GenerateAdminToken(Guid userId)
        {
            var claims = new[]
            {
                new Claim("sub", userId.ToString()),
                new Claim("role", "admin")
            };
            return Build(claims);
        }

        public static string GenerateSellerToken(Guid sellerId)
        {
            var claims = new[]
            {
                new Claim("sub", sellerId.ToString()),
                new Claim("role", "seller")
            };
            return Build(claims);
        }

        private static string Build(Claim[] claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: Issuer, audience: Audience, claims: claims,
                expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
