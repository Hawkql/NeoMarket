using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Orders.Dtos
{
    public sealed record OrderItemDto(
        Guid SkuId,
        Guid ProductId,
        string ProductTitle,
        string SkuName,
        int Quantity,
        int UnitPrice,
        int LineTotal);
}
