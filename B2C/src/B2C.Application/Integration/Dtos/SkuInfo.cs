using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// SKU (вариант товара) для покупателя.
    /// InStock = ActiveQuantity > 0 — точное число не отдаём (внутренняя информация продавца).
    /// </summary>
    public sealed record SkuInfo(
        Guid Id,
        Guid ProductId,
        string Name,
        int Price,                                              // копейки
        int Discount,                                           // процент 0..100
        string? ImageUrl,
        bool InStock,
        IReadOnlyList<CharacteristicValue> Characteristics);
}
