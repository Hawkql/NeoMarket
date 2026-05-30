using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Cart.Services
{
    /// <summary>
    /// Application-сервис разрешения "чья корзина": покупатель (по JWT)
    /// или гость (по X-Session-Id).
    /// 
    /// Зачем абстрагирован в интерфейс, а не статический класс:
    ///   - Зависит от ICurrentUserService и ISessionContext (DI)
    ///   - Зависит от ICartRepository (для lazy upsert)
    ///   - Унифицирует обработку 5+ Cart-handlers
    /// 
    /// Это не Domain Service — здесь нет cross-aggregate инвариантов.
    /// Это Application Service (orchestration), который сидит в Application слое.
    /// Реализация — рядом, в Cart/Services/CartContextResolver.cs (обычный класс,
    /// не интерфейс-реализация в Infrastructure, потому что не зависит от инфраструктуры).
    /// </summary>
    public interface ICartContextResolver
    {
        /// <summary>
        /// Получить корзину текущего инициатора. Если корзины нет — создать
        /// (lazy upsert) и вернуть пустую.
        /// 
        /// Если нет ни JWT, ни X-Session-Id — бросает DomainException("UNAUTHORIZED"):
        /// без идентификации нельзя работать с корзиной.
        /// </summary>
        Task<Domain.Carts.Cart> GetOrCreateMyCartAsync(CancellationToken ct);

        /// <summary>
        /// Получить корзину текущего инициатора. Возвращает null, если её нет.
        /// Используется в read-handlers (GET /cart): не нужно создавать пустую корзину
        /// при первом просмотре от гостя — просто вернём пустой DTO.
        /// </summary>
        Task<Domain.Carts.Cart?> GetMyCartOrNullAsync(CancellationToken ct);
    }
}
