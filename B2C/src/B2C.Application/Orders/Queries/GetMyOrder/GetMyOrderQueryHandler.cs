using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Application.Orders.Dtos;
using B2C.Domain.Buyers;
using B2C.Domain.Common;
using B2C.Domain.Orders;
using MediatR;

namespace B2C.Application.Orders.Queries.GetMyOrder
{
    public sealed class GetMyOrderQueryHandler : IRequestHandler<GetMyOrderQuery, OrderResponseDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyOrderQueryHandler(
            IOrderRepository orderRepository,
            ICurrentUserService currentUser)
        {
            _orderRepository = orderRepository;
            _currentUser = currentUser;
        }

        public async Task<OrderResponseDto> Handle(GetMyOrderQuery request, CancellationToken ct)
        {
            var buyerId = _currentUser.BuyerId;
            var order = await _orderRepository.GetByIdForBuyerAsync(
                request.OrderId, _currentUser.BuyerId, ct)
                ?? throw new DomainException("Order not found", "NOT_FOUND");
            return OrdersMapper.ToResponseDto(order);
        }
    }
}
