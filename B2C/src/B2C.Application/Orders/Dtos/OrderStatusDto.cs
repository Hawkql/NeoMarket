using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Orders.Dtos
{
    public enum OrderStatusDto
    {
        Created = 0,
        Paid = 1,
        Assembling = 2,
        Delivering = 3,
        Delivered = 4,
        Cancelled = 5,
        CancelPending = 6,
    }
}
