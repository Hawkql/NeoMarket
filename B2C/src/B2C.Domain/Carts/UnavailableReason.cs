using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Carts
{
    public enum UnavailableReason
    {
        None = 0,
        ProductBlocked = 1,
        ProductDeleted = 2,
        OutOfStock = 3,
    }
}
