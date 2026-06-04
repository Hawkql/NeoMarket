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
    /// и для авторизованного (JWT). Разрешение владельца — в ICartContextResolver.
    /// 
    /// openapi: все мутации (POST /items, PATCH /items/{sku_id}, DELETE /items/{sku_id})
    /// возвращают 200 с обновлённой CartResponse — фронт получает свежее состояние одним
    /// запросом, без отдельного GET.
    /// </summary>
    [ApiController]
    [Route("api/v1/cart")]
    [AllowAnonymous]
    public sealed class CartController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CartController(IMediator mediator) => _mediator = mediator;

        /// <summary>openapi: GET /api/v1/cart → CartResponse.</summary>
        [HttpGet]
        public async Task<IActionResult> GetCart(CancellationToken ct)
        {
            var cart = await _mediator.Send(new GetMyCartQuery(), ct);
            return Ok(cart);
        }

        /// <summary>
        /// openapi: POST /api/v1/cart/items → 200 с обновлённой CartResponse.
        /// Идемпотентно: повтор увеличивает quantity.
        /// </summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddItem(
            [FromBody] AddToCartRequest request, CancellationToken ct)
        {
            await _mediator.Send(new AddToCartCommand(request.SkuId, request.Quantity), ct);
            var cart = await _mediator.Send(new GetMyCartQuery(), ct);
            return Ok(cart);
        }

        /// <summary>
        /// openapi: PATCH /api/v1/cart/items/{sku_id} → 200 с CartResponse.
        /// </summary>
        [HttpPatch("items/{sku_id:guid}")]
        public async Task<IActionResult> UpdateItem(
            [FromRoute(Name = "sku_id")] Guid skuId,
            [FromBody] UpdateCartItemRequest request,
            CancellationToken ct)
        {
            await _mediator.Send(new UpdateCartItemQuantityCommand(skuId, request.Quantity), ct);
            var cart = await _mediator.Send(new GetMyCartQuery(), ct);
            return Ok(cart);
        }

        /// <summary>
        /// openapi: DELETE /api/v1/cart/items/{sku_id} → 200 с CartResponse (НЕ 204).
        /// </summary>
        [HttpDelete("items/{sku_id:guid}")]
        public async Task<IActionResult> RemoveItem(
            [FromRoute(Name = "sku_id")] Guid skuId, CancellationToken ct)
        {
            await _mediator.Send(new RemoveFromCartCommand(skuId), ct);
            var cart = await _mediator.Send(new GetMyCartQuery(), ct);
            return Ok(cart);
        }

        /// <summary>openapi: DELETE /api/v1/cart → 204.</summary>
        [HttpDelete]
        public async Task<IActionResult> ClearCart(CancellationToken ct)
        {
            await _mediator.Send(new ClearCartCommand(), ct);
            return NoContent();
        }
    }
}