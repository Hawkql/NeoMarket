using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Cart.Dtos
{
    /// <summary>
    /// API-представление причины недоступности позиции. Сериализуется как строка
    /// в JSON: "product_blocked", "product_deleted", "out_of_stock".
    /// </summary>
    public enum UnavailableReasonDto
    {
        None = 0,
        ProductBlocked = 1,
        ProductDeleted = 2,
        OutOfStock = 3,
    }
}
