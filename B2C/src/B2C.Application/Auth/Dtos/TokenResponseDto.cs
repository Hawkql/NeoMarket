using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Auth.Dtos
{
    /// <summary>
    /// Ответ на /auth/login, /auth/register, /auth/refresh.
    /// Формат фиксирован в OpenAPI B2C.
    /// </summary>
    public sealed record TokenResponseDto(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        string TokenType);  // всегда "Bearer"
}
