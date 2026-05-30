using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Cart.Services;
using B2C.Application.Integration;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Cart.Commands.AddToCart
{
    /// <summary>
    /// Шаги:
    ///   1. Резолвим корзину (через CartContextResolver — guest или auth).
    ///   2. Получаем SKU-info из B2B, чтобы узнать ProductId (на входе только SkuId).
    ///      Заодно проверяем, что SKU существует и доступен. Если нет — BAD_REQUEST.
    ///   3. cart.AddItem — Domain делает увеличение Quantity при повторном добавлении.
    /// 
    /// NB: НЕ проверяем "доступно ли N штук на складе" — это работа B2B при reserve.
    /// Корзина — это "wishlist with quantity". Реальная проверка остатков — при checkout.
    /// </summary>
    public sealed class AddToCartCommandHandler : IRequestHandler<AddToCartCommand>
    {
        private readonly ICartContextResolver _cartContext;
        private readonly IB2BCatalogClient _b2bCatalog;

        public AddToCartCommandHandler(
            ICartContextResolver cartContext,
            IB2BCatalogClient b2bCatalog)
        {
            _cartContext = cartContext;
            _b2bCatalog = b2bCatalog;
        }

        public async Task Handle(AddToCartCommand request, CancellationToken ct)
        {
            // 1. Резолвим корзину (создастся, если нет).
            var cart = await _cartContext.GetOrCreateMyCartAsync(ct);

            // 2. Получаем SKU из B2B (нужен ProductId — на входе только SkuId).
            var skus = await _b2bCatalog.GetSkusBatchAsync(new[] { request.SkuId }, ct);
            var sku = skus.FirstOrDefault()
                ?? throw new DomainException("SKU not found in catalog", "NOT_FOUND");

            // 3. US-CART-03 best-effort: если SKU полностью out-of-stock — сразу отказываем.
            //    Точного active_quantity у нас в API нет (коммерческая тайна продавца),
            //    поэтому проверка "N штук на складе" делегируется reserve при checkout.
            //    Этот guard ловит самый частый случай — клик «в корзину» на товар, который
            //    уже out_of_stock в B2B (например, продан между листингом и кликом).
            if (!sku.InStock)
                throw new DomainException(
                    $"SKU {sku.Id} is out of stock", "INVALID_REQUEST");

            // 4. Добавляем в корзину (Domain делает идемпотентный merge).
            cart.AddItem(sku.Id, sku.ProductId, request.Quantity);
        }
    }
}
