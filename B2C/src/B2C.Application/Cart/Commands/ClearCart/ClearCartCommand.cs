using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Cart.Commands.ClearCart
{
    /// <summary>
    /// Очистка корзины. Вызывается фронтом после успешного checkout
    /// (b2c-orders-flows: фронт сам инициирует чистку, чтобы не зависеть
    /// от race с обработкой OrderCreatedEvent).
    /// </summary>
    public sealed record ClearCartCommand : IRequest;
}
