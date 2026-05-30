using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Common.Abstractions
{
    /// <summary>
    /// Хеширование паролей покупателей. Реализация в Infrastructure (BCrypt).
    /// Application не знает алгоритма — это намеренная инверсия зависимости,
    /// чтобы при смене BCrypt → Argon2 не трогать handlers.
    /// </summary>
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }
}
