using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Products
{
    public enum ProductStatus
    {
        Created = 0,
        OnModeration = 1,
        Moderation = 2,
        Blocked = 3,
        Moderated = 4,
    }
}
