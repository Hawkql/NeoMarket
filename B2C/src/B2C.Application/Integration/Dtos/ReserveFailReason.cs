using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>Причина отказа в резервировании (от B2B).</summary>
    public enum ReserveFailReason
    {
        OutOfStock = 0,
        InsufficientStock = 1,
        ProductBlocked = 2,
        ProductDeleted = 3,
        SkuNotFound = 4,
    }
}
