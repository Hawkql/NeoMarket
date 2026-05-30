using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Security
{
    public sealed class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Secret { get; set; } = null!;
        public string Issuer { get; set; } = "b2c";
        public string Audience { get; set; } = "b2c-clients";

        /// <summary>Время жизни access-токена в минутах.</summary>
        public int AccessTokenMinutes { get; set; } = 15;

        /// <summary>Время жизни refresh-токена в днях.</summary>
        public int RefreshTokenDays { get; set; } = 30;
    }
}
