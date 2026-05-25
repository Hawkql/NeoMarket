using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.Security
{
    public sealed class JwtSettings
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string SigningKey { get; set; } = null!;
        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 30;
    }
}
