using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Значение характеристики товара или SKU (Цвет: Чёрный, Память: 256ГБ).
    /// Используется в ProductDetail и SkuInfo.
    /// </summary>
    public sealed record CharacteristicValue(string Name, string Value);
}
