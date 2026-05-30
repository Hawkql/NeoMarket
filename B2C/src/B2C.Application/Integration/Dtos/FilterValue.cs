using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Одно значение фильтра в фасете. Count — сколько товаров в текущей выборке
    /// имеют это значение (для отображения в UI: "Apple (24)").
    /// </summary>
    public sealed record FilterValue(string Value, int Count);
}
