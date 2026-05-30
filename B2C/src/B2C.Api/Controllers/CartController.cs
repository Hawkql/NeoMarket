using System;
using System.Threading;
using System.Threading.Tasks;
using B2C.Api.Contracts;
using B2C.Application.Cart.Commands.AddToCart;
using B2C.Application.Cart.Commands.ClearCart;
using B2C.Application.Cart.Commands.RemoveFromCart;
using B2C.Application.Cart.Commands.UpdateCartItemQuantity;
using B2C.Application.Cart.Queries.GetMyCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    /// <summary>
    /// Корзина (US-CART-03). НЕ требует [Authorize]: работает для гостя (X-Session-Id)
    /// и для авторизованного (JWT). Разрешение владельца корзины — в ICartContextResolver.
    /// 
    /// Если запрос пришёл без JWT и без X-Session-Id — ICartContextResolver бросит
    /// UNAUTHORIZED (→ 401). Это правильно: с корзиной нельзя работать анонимно
    /// без хоть какого-то идентификатора.
    /// </summary>
    [ApiController]
    [Route("api/v1/cart")]
    [AllowAnonymous]
    public sealed class CartController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CartController(IMediator mediator) => _mediator = mediator;

        /// <summary>Получить корзину с актуальными ценами из B2B (US-CART-03).</summary>
        [HttpGet]
        public async Task<IActionResult> GetCart(CancellationToken ct)
        {
            var cart = await _mediator.Send(new GetMyCartQuery(), ct);
            return Ok(cart);
        }

        /// <summary>Добавить SKU (идемпотентно — повтор увеличивает quantity).</summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddItem(
            [FromBody] AddToCartRequest request, CancellationToken ct)
        {
            await _mediator.Send(new AddToCartCommand(request.SkuId, request.Quantity), ct);
            return NoContent();
        }

        /// <summary>Изменить количество позиции.</summary>
        [HttpPut("items/{skuId:guid}")]
        public async Task<IActionResult> UpdateItem(
            Guid skuId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
        {
            await _mediator.Send(new UpdateCartItemQuantityCommand(skuId, request.Quantity), ct);
            return NoContent();
        }

        /// <summary>Удалить позицию (идемпотентно).</summary>
        [HttpDelete("items/{skuId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid skuId, CancellationToken ct)
        {
            await _mediator.Send(new RemoveFromCartCommand(skuId), ct);
            return NoContent();
        }

        /// <summary>Очистить корзину (фронт вызывает после успешного checkout).</summary>
        [HttpDelete]
        public async Task<IActionResult> ClearCart(CancellationToken ct)
        {
            await _mediator.Send(new ClearCartCommand(), ct);
            return NoContent();
        }
    }
}