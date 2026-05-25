using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace B2B.Infrastructure.Security
{
    public sealed class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public TokenPair GenerateTokens(Guid sellerId, string email)
        {
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_settings.AccessTokenMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, sellerId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("role", "seller"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.SigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            // refresh — криптослучайная строка (не JWT), хранится только как хэш
            var refreshToken = GenerateSecureToken();
            var refreshHash = HashRefreshToken(refreshToken);

            return new TokenPair(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                RefreshTokenHash: refreshHash,
                ExpiresInSeconds: _settings.AccessTokenMinutes * 60);
        }

        public string HashRefreshToken(string refreshToken)
        {
            // SHA256 достаточно: refresh — высокоэнтропийная случайная строка,
            // не подвержена brute-force как пароль (BCrypt тут избыточен).
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(bytes);
        }

        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }
    }
}
