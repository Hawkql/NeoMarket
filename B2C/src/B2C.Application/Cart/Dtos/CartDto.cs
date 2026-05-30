using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Cart.Dtos
{
    /// <summary>
    /// Корзина для покупателя. TotalAmount — сумма по AVAILABLE items.
    /// Недоступные товары в сумму не входят (но в списке Items остаются).
    /// </summary>
    public sealed record CartDto(
        IReadOnlyList<CartItemDto> Items,
        int TotalAmount,            // копейки, только по available items
        int ItemsCount,             // суммарное количество единиц (sum of quantity) по available
        int AvailableItemsCount,    // количество разных SKU, доступных
        int UnavailableItemsCount); // количество разных SKU, недоступных
}
