using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Subscriptions
{
    [System.Flags]
    public enum NotifyOn
    {
        None = 0,
        InStock = 1 << 0,  // товар снова в наличии
        PriceDrop = 1 << 1,  // цена снижена
    }
}
