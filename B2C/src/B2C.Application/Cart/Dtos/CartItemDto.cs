using System;
using B2C.Application.Catalog.Dtos;

namespace B2C.Application.Cart.Dtos
{
    /// <summary>
    /// openapi: CartItem. required: sku_id, product_id, name, quantity,
    /// unit_price, line_total, available_quantity, is_available.
    /// Optional: sku_code, unit_price_at_add, image.
    /// 
    /// name = "{ProductTitle} {SkuName}".Trim() — одна строка, как требует openapi.
    /// image — ImageRef (singular), не массив. Если изображения нет — null.
    /// is_available = UnavailableReason == None AND quantity ≤ available_quantity.
    /// </summary>
    public sealed record CartItemDto(
        Guid SkuId,
        Guid ProductId,
        string Name,
        string? SkuCode,
        int Quantity,
        int UnitPrice,
        int? UnitPriceAtAdd,
        int LineTotal,
        int AvailableQuantity,
        bool IsAvailable,
        ImageRefDto? Image);
}