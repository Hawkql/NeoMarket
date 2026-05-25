using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Common.Abstractions
{
    public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    string RefreshTokenHash,   
    int ExpiresInSeconds);

    public interface ITokenService
    {
        /// <summary>Генерирует access (JWT) + refresh (случайный) + хэш refresh для хранения.</summary>
        TokenPair GenerateTokens(Guid sellerId, string email);

        /// <summary>Хэш произвольного refresh-токена (для поиска в БД при refresh/logout).</summary>
        string HashRefreshToken(string refreshToken);
    }
}
