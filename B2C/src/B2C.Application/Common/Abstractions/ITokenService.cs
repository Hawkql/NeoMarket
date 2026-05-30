using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Common.Abstractions
{
    /// <summary>
    /// Пара токенов: access (JWT, короткоживущий, ~15 минут),
    /// refresh (случайная строка, хранится как ХЭШ — при утечке БД нельзя использовать),
    /// RefreshTokenHash — то, что Application передаёт в Domain.RefreshToken(...).
    /// </summary>
    public sealed record TokenPair(
        string AccessToken,
        string RefreshToken,
        string RefreshTokenHash,
        int ExpiresInSeconds);

    public interface ITokenService
    {
        /// <summary>Генерирует access (JWT) + refresh (случайный) + хэш refresh для хранения.</summary>
        TokenPair GenerateTokens(Guid buyerId, string email);

        /// <summary>Хэш произвольного refresh-токена (для поиска в БД при refresh/logout).</summary>
        string HashRefreshToken(string refreshToken);
    }
}
