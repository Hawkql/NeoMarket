using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace B2C.Infrastructure.Security
{
    /// <summary>
    /// Реализация ITokenService.
    /// 
    /// Access — JWT, подписанный СВОИМ секретом B2C. Claims: sub=BuyerId, email.
    /// Refresh — случайные 32 байта в base64url. В БД хранится только SHA256-хэш
    /// (при утечке БД refresh-токены нельзя использовать).
    /// </summary>
    public sealed class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public TokenPair GenerateTokens(Guid buyerId, string email)
        {
            var accessToken = GenerateAccessToken(buyerId, email);

            // Refresh — криптослучайная строка, не JWT.
            var refreshRaw = GenerateSecureRandomString();
            var refreshHash = HashRefreshToken(refreshRaw);

            var expiresIn = _settings.AccessTokenMinutes * 60;

            return new TokenPair(accessToken, refreshRaw, refreshHash, expiresIn);
        }

        public string HashRefreshToken(string refreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(bytes);  // стабильный hex-формат для поиска по БД
        }

        private string GenerateAccessToken(Guid buyerId, string email)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, buyerId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateSecureRandomString()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            // base64url — безопасно для передачи в JSON/заголовках.
            return Convert.ToBase64String(bytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }
}
