using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Common.Abstractions
{
    /// <summary>
    /// Доступ к данным текущего аутентифицированного покупателя.
    /// 
    /// КРИТИЧНО для IDOR-защиты (см. b2c-orders-flows.md):
    /// BuyerId извлекается ТОЛЬКО из JWT claims, никогда не из request body / query.
    /// Это правило для всех buyer-facing endpoints (orders, favorites, cart, addresses).
    /// 
    /// IsAuthenticated = false → запрос анонимный (гостевая корзина, публичный каталог).
    /// </summary>
    public interface ICurrentUserService
    {
        Guid BuyerId { get; }
        bool IsAuthenticated { get; }
    }
}
