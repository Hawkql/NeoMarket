using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration.Dtos;

using B2C.Domain.Carts;

namespace B2C.Application.Cart.Dtos
{
    internal static class CartMapper
    {
        public static UnavailableReasonDto ToDto(UnavailableReason domain) => domain switch
        {
            UnavailableReason.None => UnavailableReasonDto.None,
            UnavailableReason.ProductBlocked => UnavailableReasonDto.ProductBlocked,
            UnavailableReason.ProductDeleted => UnavailableReasonDto.ProductDeleted,
            UnavailableReason.OutOfStock => UnavailableReasonDto.OutOfStock,
            _ => UnavailableReasonDto.None,
        };

        /// <summary>
        /// Маппинг позиции корзины с опциональным обогащением SKU-данными из B2B.
        /// sku может быть null — если B2B-каталог не нашёл SKU (товар удалён).
        /// </summary>
        public static CartItemDto ToItemDto(CartItem item, SkuInfo? sku, ProductSummary? product)
        {
            var unitPrice = sku?.Price;
            var lineTotal = unitPrice.HasValue ? unitPrice.Value * item.Quantity : (int?)null;

            // UnavailableReason: сначала смотрим сохранённый в БД (event-driven из B2B),
            // затем дополняем runtime-проверкой из свежего ответа каталога —
            // US-CART-03: доступность обновляется при каждом просмотре корзины.
            var unavailable = ToDto(item.UnavailableReason);
            if (unavailable == UnavailableReasonDto.None)
            {
                if (sku is null)
                    unavailable = UnavailableReasonDto.ProductDeleted;
                else if (!sku.InStock)
                    unavailable = UnavailableReasonDto.OutOfStock;
            }

            return new CartItemDto(
                SkuId: item.SkuId,
                ProductId: item.ProductId,
                Title: product?.Title,
                SkuName: sku?.Name,
                ImageUrl: sku?.ImageUrl ?? product?.ImageUrl,
                UnitPrice: unitPrice,
                Quantity: item.Quantity,
                LineTotal: lineTotal,
                UnavailableReason: unavailable);
        }

        /// <summary>
        /// Сборка CartDto из набора замапленных позиций.
        /// Total/Counts считаются здесь — это presentation-логика, не доменная.
        /// </summary>
        public static CartDto ToCartDto(IReadOnlyList<CartItemDto> items)
        {
            var available = items.Where(i => i.UnavailableReason == UnavailableReasonDto.None).ToList();
            var unavailable = items.Where(i => i.UnavailableReason != UnavailableReasonDto.None).ToList();

            var total = available.Sum(i => i.LineTotal ?? 0);
            var itemsCount = available.Sum(i => i.Quantity);

            return new CartDto(
                Items: items,
                TotalAmount: total,
                ItemsCount: itemsCount,
                AvailableItemsCount: available.Count,
                UnavailableItemsCount: unavailable.Count);
        }
    }
}
