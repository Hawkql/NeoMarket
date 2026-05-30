using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Краткие данные товара для списков (каталог, избранное, корзина, главная подборки).
    /// ACL-фильтрованный набор полей: НЕТ cost_price, reserved_quantity (US-CAT-03).
    /// </summary>
    public sealed record ProductSummary(
        Guid Id,
        string Title,
        string? ImageUrl,
        int Price,           // копейки, минимальная цена среди SKU
        int? OldPrice,       // копейки, для отображения зачёркнутой цены
        int? Discount,       // процент 0..100
        bool InStock,
        double? Rating,
        int? ReviewsCount);
}
