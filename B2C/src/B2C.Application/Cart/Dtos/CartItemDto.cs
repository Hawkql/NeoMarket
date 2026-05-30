using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Cart.Dtos
{
    /// <summary>
    /// Позиция корзины с обогащёнными данными из B2B.
    /// 
    /// UnitPrice/Title/SkuName/ImageUrl — приходят из B2B при GET. Если товар
    /// удалён в B2B (нечего обогащать) — все поля null/пустые, но Quantity и SkuId
    /// сохраняются, чтобы фронт мог отобразить заглушку с UnavailableReason.
    /// 
    /// LineTotal вычисляется в Application (UnitPrice * Quantity), фронт его не считает.
    /// </summary>
    public sealed record CartItemDto(
        Guid SkuId,
        Guid ProductId,
        string? Title,
        string? SkuName,
        string? ImageUrl,
        int? UnitPrice,        // null если товар недоступен в B2B
        int Quantity,
        int? LineTotal,        // null если UnitPrice == null
        UnavailableReasonDto UnavailableReason);
}
