using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Orders;

namespace B2C.Application.Orders.Dtos
{
    internal static class OrdersMapper
    {
        public static OrderStatusDto ToStatusDto(OrderStatus status) => status switch
        {
            OrderStatus.Created => OrderStatusDto.Created,
            OrderStatus.Paid => OrderStatusDto.Paid,
            OrderStatus.Assembling => OrderStatusDto.Assembling,
            OrderStatus.Delivering => OrderStatusDto.Delivering,
            OrderStatus.Delivered => OrderStatusDto.Delivered,
            OrderStatus.Cancelled => OrderStatusDto.Cancelled,
            OrderStatus.CancelPending => OrderStatusDto.CancelPending,
            _ => OrderStatusDto.Created,
        };

        public static OrderItemDto ToItemDto(OrderItem i) =>
            new(i.SkuId, i.ProductId, i.ProductTitle, i.SkuName,
                i.Quantity, i.UnitPrice, i.LineTotal);

        public static OrderSummaryDto ToSummaryDto(Order o) =>
            new(o.Id,
                ToStatusDto(o.Status),
                o.TotalAmount,
                o.Items.Sum(i => i.Quantity),
                o.CreatedAt);

        public static OrderDetailDto ToDetailDto(Order o) =>
            new(o.Id,
                ToStatusDto(o.Status),
                o.TotalAmount,
                o.DeliveryAddress.Value,
                o.Items.Select(ToItemDto).ToList(),
                o.CreatedAt,
                o.UpdatedAt);
    }
}
