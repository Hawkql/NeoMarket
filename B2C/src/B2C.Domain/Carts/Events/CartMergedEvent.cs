using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Carts.Events
{
    /// <summary>
    /// Гостевая корзина слита в авторизованную. Используется для аудита.
    /// MergedItemCount — сколько позиций было перенесено (с учётом MAX-merge).
    /// </summary>
    public record CartMergedEvent(Guid TargetCartId, Guid SourceCartId, int MergedItemCount) : DomainEvent;
}
