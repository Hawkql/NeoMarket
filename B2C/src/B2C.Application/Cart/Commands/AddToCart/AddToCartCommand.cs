using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Cart.Commands.AddToCart
{
    /// <summary>
    /// Добавление SKU в корзину.
    /// Owner (BuyerId/SessionId) определяется ICartContextResolver — в команду не пробрасываем.
    /// </summary>
    public sealed record AddToCartCommand(
        Guid SkuId,
        int Quantity) : IRequest;
}
