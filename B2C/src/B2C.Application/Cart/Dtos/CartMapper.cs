using System;
using System.Collections.Generic;
using System.Linq;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Integration.Dtos;
using B2C.Domain.Carts;

namespace B2C.Application.Cart.Dtos
{
    internal static class CartMapper
    {
        /// <summary>
        /// Маппинг позиции корзины + опционального обогащения SKU/Product из B2B.
        /// 
        /// sku == null → товар удалён в B2B. Возвращаем placeholder с UnitPrice=0,
        /// AvailableQuantity=0, IsAvailable=false — фронт покажет заглушку.
        /// 
        /// is_available = домен (CartItem.UnavailableReason != None уже значит false)
        /// ИЛИ runtime-проверка: SKU нет, InStock=false, Quantity > AvailableQuantity.
        /// </summary>
        public static CartItemDto ToItemDto(CartItem item, SkuInfo? sku, ProductSummary? product)
        {
            // Если B2B не вернул SKU — товар удалён/недоступен.
            if (sku is null)
            {
                return new CartItemDto(
                    SkuId: item.SkuId,
                    ProductId: item.ProductId,
                    Name: product?.Title ?? "Товар недоступен",
                    SkuCode: null,
                    Quantity: item.Quantity,
                    UnitPrice: 0,
                    UnitPriceAtAdd: null,
                    LineTotal: 0,
                    AvailableQuantity: 0,
                    IsAvailable: false,
                    Image: null);
            }

            // Композитное имя по openapi: "товар + SKU".
            var name = string.IsNullOrWhiteSpace(sku.Name)
                ? (product?.Title ?? string.Empty)
                : $"{product?.Title} {sku.Name}".Trim();

            // is_available учитывает: доменный статус, runtime in-stock, доступное кол-во.
            var isAvailable =
                item.UnavailableReason == UnavailableReason.None
                && sku.InStock
                && item.Quantity <= sku.AvailableQuantity;

            var unitPrice = sku.Price;
            var lineTotal = unitPrice * item.Quantity;

            // ImageRef singular: первое изображение SKU, иначе изображение продукта.
            var imageUrl = sku.ImageUrl ?? product?.ImageUrl;
            ImageRefDto? image = imageUrl is null
                ? null
                : new ImageRefDto(
                    Id: GuidFromUrl(imageUrl),
                    Url: imageUrl,
                    Ordering: 0,
                    Alt: null);

            return new CartItemDto(
                SkuId: item.SkuId,
                ProductId: item.ProductId,
                Name: name,
                SkuCode: null,                // B2B пока не отдаёт sku_code в SkuInfo
                Quantity: item.Quantity,
                UnitPrice: unitPrice,
                UnitPriceAtAdd: null,         // B2B не хранит snapshot цены добавления
                LineTotal: lineTotal,
                AvailableQuantity: sku.AvailableQuantity,
                IsAvailable: isAvailable,
                Image: image);
        }

        /// <summary>
        /// Сборка CartDto по openapi: items + items_count + subtotal + is_valid.
        /// Subtotal — только по AVAILABLE items (по US-CART-03 acceptance).
        /// is_valid — true ⟺ все items.is_available.
        /// </summary>
        public static CartDto ToCartDto(
            Guid cartId,
            DateTime updatedAt,
            IReadOnlyList<CartItemDto> items)
        {
            var available = items.Where(i => i.IsAvailable).ToList();
            var subtotal = available.Sum(i => i.LineTotal);
            var itemsCount = available.Sum(i => i.Quantity);
            var isValid = items.All(i => i.IsAvailable);

            return new CartDto(
                Id: cartId,
                Items: items,
                ItemsCount: itemsCount,
                Subtotal: subtotal,
                IsValid: isValid,
                UpdatedAt: updatedAt);
        }

        // Детерминированный uuid из URL — как в CatalogMapper.
        private static Guid GuidFromUrl(string url)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
            return new Guid(hash);
        }
    }
}