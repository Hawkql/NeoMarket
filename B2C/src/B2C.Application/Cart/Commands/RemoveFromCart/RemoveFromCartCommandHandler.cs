using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Cart.Services;
using MediatR;

namespace B2C.Application.Cart.Commands.RemoveFromCart
{
    /// <summary>
    /// Идемпотентно: нет корзины или нет позиции — успешный 204.
    /// (DELETE-семантика).
    /// </summary>
    public sealed class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand>
    {
        private readonly ICartContextResolver _cartContext;

        public RemoveFromCartCommandHandler(ICartContextResolver cartContext)
        {
            _cartContext = cartContext;
        }

        public async Task Handle(RemoveFromCartCommand request, CancellationToken ct)
        {
            var cart = await _cartContext.GetMyCartOrNullAsync(ct);
            if (cart is null) return;

            cart.RemoveItem(request.SkuId);  // Domain.Cart.RemoveItem — idempotent
        }
    }
}
