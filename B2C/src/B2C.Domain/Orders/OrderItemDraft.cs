using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Orders
{
    /// <summary>
    /// Parameter Object для передачи валидированных данных позиции в Order.Create.
    /// 
    /// Не DTO: не пересекает границу слоя, не сериализуется. Это группировка
    /// 6 параметров фабрики (рефакторинг "Introduce Parameter Object", Fowler).
    /// 
    /// Поток использования (только Application → Domain):
    ///   1. CreateOrderCommandHandler собирает Draft из обогащённых B2B-данных
    ///   2. Передаёт коллекцию в Order.Create
    ///   3. Domain создаёт OrderItem-снимки и больше Draft'ы нигде не использует
    /// </summary>
    public sealed record OrderItemDraft(
        Guid SkuId,
        Guid ProductId,
        string ProductTitle,
        string SkuName,
        int Quantity,
        int UnitPrice);
}
