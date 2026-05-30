using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace B2C.Api.Tests.Infrastructure
{
    public static class JwtTokenHelper
    {
        // Должен совпадать с Jwt:Secret в CustomWebApplicationFactory.
        private const string Secret =
            "b2c-jwt-signing-key-must-be-at-least-32-chars-long-test";
        private const string Issuer = "neomarket-b2c";
        private const string Audience = "neomarket";

        /// <summary>Токен покупателя. sub-claim = BuyerId (читается CurrentUserService).</summary>
        public static string GenerateBuyerToken(Guid buyerId)
        {
            var claims = new[]
            {
                new Claim("sub", buyerId.ToString()),
                new Claim("email", $"buyer-{buyerId:N}@test.local"),
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
