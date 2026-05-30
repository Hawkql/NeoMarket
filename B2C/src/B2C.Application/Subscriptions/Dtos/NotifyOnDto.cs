using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Subscriptions.Dtos
{
    /// <summary>
    /// API-уровневое представление notify_on. Flags-enum: значения комбинируются битовым OR.
    /// Например, чтобы подписаться и на InStock, и на PriceDrop: NotifyOnDto.InStock | NotifyOnDto.PriceDrop.
    /// 
    /// Отделён от Domain.NotifyOn ради независимости API от внутреннего контракта.
    /// </summary>
    [System.Flags]
    public enum NotifyOnDto
    {
        None = 0,
        InStock = 1 << 0,
        PriceDrop = 1 << 1,
    }
}
