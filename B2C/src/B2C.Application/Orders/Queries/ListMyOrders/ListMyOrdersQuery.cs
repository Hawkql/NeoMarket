using B2C.Application.Common.Pagination;
using B2C.Application.Orders.Dtos;
using MediatR;

namespace B2C.Application.Orders.Queries.ListMyOrders
{
    public sealed record ListMyOrdersQuery(
        OrderStatusDto? StatusFilter,
        int Limit,
        int Offset) : IRequest<PagedResult<OrderResponseDto>>;
}