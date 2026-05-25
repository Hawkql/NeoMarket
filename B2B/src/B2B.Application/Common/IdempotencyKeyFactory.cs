using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Common
{
    /// <summary>
    /// Детерминированный Guid из строки. Один вход → один Guid (стабильно между
    /// запросами). Используется, чтобы развести идемпотентность операций с общим
    /// order_id (unreserve vs fulfill).
    /// </summary>
    public static class IdempotencyKeyFactory
    {
        public static Guid FromParts(params string[] parts)
        {
            var input = string.Join(":", parts);
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            // Берём первые 16 байт хэша как Guid
            return new Guid(bytes.AsSpan(0, 16));
        }
    }
}
