using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
namespace B2C.Application.Common
{
    /// <summary>
    /// Детерминированный Guid из строки. Один вход → один Guid (стабильно между
    /// запросами и перезапусками). Используется, чтобы развести идемпотентность
    /// операций с общим идентификатором (например, unreserve и fulfill по order_id).
    /// 
    /// Не криптографически защищённый ID — SHA256 здесь как хеш-функция для
    /// детерминированного producing 16 байт, а не как защита от подделки.
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
