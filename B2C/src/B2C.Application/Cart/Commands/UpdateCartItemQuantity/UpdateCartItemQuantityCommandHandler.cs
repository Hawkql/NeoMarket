using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Cart.Services;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Cart.Commands.UpdateCartItemQuantity
{
    public sealed class UpdateCartItemQuantityCommandHandler
        : IRequestHandler<UpdateCartItemQuantityCommand>
    {
        private readonly ICartContextResolver _cartContext;

        public UpdateCartItemQuantityCommandHandler(ICartContextResolver cartContext)
        {
            _cartContext = cartContext;
        }

        public async Task Handle(UpdateCartItemQuantityCommand request, CancellationToken ct)
        {
            // Корзина должна существовать — нельзя обновить позицию в несуществующей.
            var cart = await _cartContext.GetMyCartOrNullAsync(ct)
                ?? throw new DomainException("Cart is empty", "NOT_FOUND");

            // Domain бросит NOT_FOUND, если SKU нет в корзине — это правильно.
            cart.SetItemQuantity(request.SkuId, request.Quantity);
        }
    }
}
