using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Cart.Dtos;
using B2C.Application.Cart.Services;
using B2C.Application.Integration;
using MediatR;

namespace B2C.Application.Cart.Queries.GetMyCart
{
    /// <summary>
    /// Шаги:
    ///   1. Корзина current owner (или null, если нет — вернём пустой CartDto).
    ///   2. Batch-обогащение из B2B: SKUs (цена, наличие, имя) + Products (title, image).
    ///      Делаем ДВА запроса параллельно через Task.WhenAll — экономия latency.
    ///   3. Маппинг с tolerance: если B2B не вернул SKU (товар удалён), unitPrice=null,
    ///      title=null — фронт покажет заглушку по UnavailableReason.
    /// 
    /// NB: цены ВСЕГДА свежие — мы не кэшируем их в корзине (US-CART-03).
    /// </summary>
    public sealed class GetMyCartQueryHandler : IRequestHandler<GetMyCartQuery, CartDto>
    {
        private readonly ICartContextResolver _cartContext;
        private readonly IB2BCatalogClient _b2bCatalog;

        public GetMyCartQueryHandler(
            ICartContextResolver cartContext,
            IB2BCatalogClient b2bCatalog)
        {
            _cartContext = cartContext;
            _b2bCatalog = b2bCatalog;
        }

        public async Task<CartDto> Handle(GetMyCartQuery request, CancellationToken ct)
        {
            var cart = await _cartContext.GetMyCartOrNullAsync(ct);

            // Гость без корзины или auth-пользователь без корзины — пустой CartDto.
            if (cart is null || cart.Items.Count == 0)
                return CartMapper.ToCartDto(Array.Empty<CartItemDto>());

            var skuIds = cart.Items.Select(i => i.SkuId).Distinct().ToList();
            var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();

            // Параллельные запросы — независимые батчи.
            var skusTask = _b2bCatalog.GetSkusBatchAsync(skuIds, ct);
            var productsTask = _b2bCatalog.GetProductsBatchAsync(productIds, ct);
            await Task.WhenAll(skusTask, productsTask);

            var skusById = (await skusTask).ToDictionary(s => s.Id);
            var productsById = (await productsTask).ToDictionary(p => p.Id);

            var itemDtos = cart.Items.Select(item =>
            {
                skusById.TryGetValue(item.SkuId, out var sku);
                productsById.TryGetValue(item.ProductId, out var product);
                return CartMapper.ToItemDto(item, sku, product);
            }).ToList();

            return CartMapper.ToCartDto(itemDtos);
        }
    }
}