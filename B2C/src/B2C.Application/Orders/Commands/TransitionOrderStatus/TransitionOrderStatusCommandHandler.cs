using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Orders.Dtos;
using B2C.Domain.Common;
using B2C.Domain.Orders;
using MediatR;

namespace B2C.Application.Orders.Commands.TransitionOrderStatus
{
    public sealed class TransitionOrderStatusCommandHandler
      : IRequestHandler<TransitionOrderStatusCommand, OrderResponseDto>
    {
        private readonly IOrderRepository _orderRepository;

        public TransitionOrderStatusCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<OrderResponseDto> Handle(
            TransitionOrderStatusCommand request, CancellationToken ct)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, ct)
                ?? throw new DomainException("Order not found", "NOT_FOUND");

            switch (request.TargetStatus)
            {
                case OrderStatusDto.Assembling:
                    order.StartAssembling();
                    break;
                case OrderStatusDto.Delivering:
                    order.StartDelivering();
                    break;
                case OrderStatusDto.Delivered:
                    order.MarkAsDelivered();   // поднимет OrderDeliveredEvent → fulfill
                    break;
                default:
                    throw new DomainException(
                        $"Unsupported target status {request.TargetStatus}", "INVALID_REQUEST");
            }

            return OrdersMapper.ToResponseDto(order);
        }
    }
}