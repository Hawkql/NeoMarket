using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;

namespace B2B.Infrastructure.Security
{
    public sealed class BcryptPasswordHasher : IPasswordHasher
    {
        // workFactor 12 — баланс безопасности/скорости для прода
        private const int WorkFactor = 12;

        public string Hash(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        public bool Verify(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                // Повреждённый/невалидный хэш — не аутентифицируем
                return false;
            }
        }
    }
}
