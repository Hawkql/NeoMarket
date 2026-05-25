using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Auth.Dtos
{
    public sealed record TokenResponseDto(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    string TokenType,        
    int ExpiresIn);
}
