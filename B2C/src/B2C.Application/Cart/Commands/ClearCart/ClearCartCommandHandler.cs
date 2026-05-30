using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Cart.Services;
using MediatR;

namespace B2C.Application.Cart.Commands.ClearCart
{
    public sealed class ClearCartCommandHandler : IRequestHandler<ClearCartCommand>
    {
        private readonly ICartContextResolver _cartContext;

        public ClearCartCommandHandler(ICartContextResolver cartContext)
        {
            _cartContext = cartContext;
        }

        public async Task Handle(ClearCartCommand request, CancellationToken ct)
        {
            var cart = await _cartContext.GetMyCartOrNullAsync(ct);
            if (cart is null) return;

            cart.Clear();
        }
    }
}
