using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Orders.Dtos
{
    public sealed record OrderDetailDto(
       Guid Id,
       OrderStatusDto Status,
       int TotalAmount,
       string DeliveryAddress,
       IReadOnlyList<OrderItemDto> Items,
       DateTime CreatedAt,
       DateTime UpdatedAt);
}
