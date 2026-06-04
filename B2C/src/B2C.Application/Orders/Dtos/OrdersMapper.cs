using System.Linq;
using B2C.Domain.Orders;

namespace B2C.Application.Orders.Dtos
{
    internal static class OrdersMapper
    {
        /// <summary>
        /// openapi status — UPPER_SNAKE_CASE: CREATED, PAID, ASSEMBLING, DELIVERING,
        /// DELIVERED, CANCELLED, CANCEL_PENDING.
        /// </summary>
        public static string ToStatusString(OrderStatus s) => s switch
        {
            OrderStatus.Created => "CREATED",
            OrderStatus.Paid => "PAID",
            OrderStatus.Assembling => "ASSEMBLING",
            OrderStatus.Delivering => "DELIVERING",
            OrderStatus.Delivered => "DELIVERED",
            OrderStatus.Cancelled => "CANCELLED",
            OrderStatus.CancelPending => "CANCEL_PENDING",
            _ => s.ToString().ToUpperInvariant(),
        };

        public static OrderItemDto ToItemDto(OrderItem i)
        {
            // openapi: name — одна строка "ProductTitle SkuName".
            var name = string.IsNullOrWhiteSpace(i.SkuName)
                ? i.ProductTitle
                : $"{i.ProductTitle} {i.SkuName}".Trim();

            return new OrderItemDto(
                SkuId: i.SkuId,
                ProductId: i.ProductId,
                Name: name,
                SkuCode: null,                  // B2B не отдаёт; для расширения
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice,
                LineTotal: i.LineTotal);
        }

        public static AddressDto ToAddressDto(OrderAddress a, System.DateTime orderCreatedAt) =>
            new(
                Id: a.OriginalAddressId ?? System.Guid.Empty,
                Country: a.Country,
                City: a.City,
                Street: a.Street,
                House: a.House,
                Apartment: a.Apartment,
                PostalCode: a.PostalCode,
                CreatedAt: orderCreatedAt);

        public static PaymentMethodDto ToPaymentMethodDto(Order o) =>
            new(
                Id: o.PaymentMethodId,
                Type: o.PaymentMethodType,
                CreatedAt: o.CreatedAt);   // отдельной даты нет — берём дату заказа

        /// <summary>
        /// Единый маппер по openapi OrderResponse — на list и на detail.
        /// </summary>
        public static OrderResponseDto ToResponseDto(Order o) =>
            new(
                Id: o.Id,
                Number: null,                                  // human-readable номер пока не генерим
                BuyerId: o.BuyerId,
                Status: ToStatusString(o.Status),
                StatusHistory: null,                           // history пока не ведём
                Items: o.Items.Select(ToItemDto).ToList(),
                Subtotal: o.Subtotal,
                DeliveryCost: o.DeliveryCost,
                Total: o.Total,
                Address: ToAddressDto(o.Address, o.CreatedAt),
                PaymentMethod: ToPaymentMethodDto(o),
                Comment: o.Comment,
                CancelReason: o.CancelReason,
                CreatedAt: o.CreatedAt,
                PaidAt: o.PaidAt,
                DeliveredAt: o.DeliveredAt);
    }
}