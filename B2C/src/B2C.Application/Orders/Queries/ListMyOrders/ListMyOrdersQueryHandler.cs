using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Application.Common.Pagination;
using B2C.Application.Orders.Dtos;
using B2C.Domain.Orders;
using MediatR;

namespace B2C.Application.Orders.Queries.ListMyOrders
{
    public sealed class ListMyOrdersQueryHandler
        : IRequestHandler<ListMyOrdersQuery, PagedResult<OrderResponseDto>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICurrentUserService _currentUser;

        public ListMyOrdersQueryHandler(
            IOrderRepository orderRepository,
            ICurrentUserService currentUser)
        {
            _orderRepository = orderRepository;
            _currentUser = currentUser;
        }

        public async Task<PagedResult<OrderResponseDto>> Handle(
            ListMyOrdersQuery request, CancellationToken ct)
        {
            OrderStatus? statusFilter = request.StatusFilter is null
                ? null
                : MapStatus(request.StatusFilter.Value);

            var (orders, total) = await _orderRepository.ListByBuyerAsync(
                _currentUser.BuyerId, statusFilter, request.Limit, request.Offset, ct);

            return new PagedResult<OrderResponseDto>(
                orders.Select(OrdersMapper.ToResponseDto).ToList(),
                total,
                request.Limit,
                request.Offset);
        }

        private static OrderStatus MapStatus(OrderStatusDto dto) => dto switch
        {
            OrderStatusDto.Created => OrderStatus.Created,
            OrderStatusDto.Paid => OrderStatus.Paid,
            OrderStatusDto.Assembling => OrderStatus.Assembling,
            OrderStatusDto.Delivering => OrderStatus.Delivering,
            OrderStatusDto.Delivered => OrderStatus.Delivered,
            OrderStatusDto.Cancelled => OrderStatus.Cancelled,
            OrderStatusDto.CancelPending => OrderStatus.CancelPending,
            _ => OrderStatus.Created,
        };
    }
}
