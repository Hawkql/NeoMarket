using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;
using B2C.Application.Orders.Dtos;
using B2C.Domain.Common;
using B2C.Domain.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.Orders.Commands.CreateOrder
{
    public sealed class CreateOrderCommandHandler
        : IRequestHandler<CreateOrderCommand, OrderDetailDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IB2BCatalogClient _b2bCatalog;
        private readonly IB2BReservationClient _b2bReservation;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(
            IOrderRepository orderRepository,
            IB2BCatalogClient b2bCatalog,
            IB2BReservationClient b2bReservation,
            ICurrentUserService currentUser,
            ILogger<CreateOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _b2bCatalog = b2bCatalog;
            _b2bReservation = b2bReservation;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<OrderDetailDto> Handle(CreateOrderCommand request, CancellationToken ct)
        {
            var buyerId = _currentUser.BuyerId;
            var idempotencyKey = IdempotencyKey.From(request.IdempotencyKey);

            // 1. Idempotency check.
            var existing = await _orderRepository.GetByIdempotencyKeyAsync(buyerId, idempotencyKey, ct);
            if (existing is not null)
            {
                _logger.LogInformation(
                    "Idempotent checkout: returning existing order {OrderId} for key {Key}",
                    existing.Id, request.IdempotencyKey);
                return OrdersMapper.ToDetailDto(existing);
            }

            // 2. Snapshot из B2B: цены + ProductId + названия.
            var skuIds = request.Items.Select(i => i.SkuId).Distinct().ToList();
            var skus = await _b2bCatalog.GetSkusBatchAsync(skuIds, ct);
            var skusById = skus.ToDictionary(s => s.Id);

            // Проверка: все запрошенные SKU существуют.
            var missing = skuIds.Where(id => !skusById.ContainsKey(id)).ToList();
            if (missing.Any())
                throw new DomainException(
                    $"SKUs not found in catalog: {string.Join(", ", missing)}", "INVALID_REQUEST");

            // Нужны названия товаров для snapshot — догружаем продукты.
            var productIds = skus.Select(s => s.ProductId).Distinct().ToList();
            var products = await _b2bCatalog.GetProductsBatchAsync(productIds, ct);
            var productsById = products.ToDictionary(p => p.Id);

            // 3. Reserve в B2B (ДО создания Order).
            var reserveLines = request.Items
                .Select(i => new ReserveLine(i.SkuId, i.Quantity))
                .ToList();

            var reserveResult = await _b2bReservation.ReserveAsync(
                request.IdempotencyKey, reserveLines, ct);

            if (!reserveResult.Success)
            {
                // 4b. Reserve упал — заказ не создаётся. Детализация в сообщении.
                var failDetails = string.Join("; ", reserveResult.FailedItems.Select(
                    f => $"sku {f.SkuId}: requested {f.Requested}, available {f.Available} ({f.Reason})"));

                _logger.LogWarning(
                    "Reserve failed for buyer {BuyerId}, key {Key}: {Details}",
                    buyerId, request.IdempotencyKey, failDetails);

                throw new DomainException(
                    $"Cannot reserve items: {failDetails}", "CONFLICT");
            }

            // 4a. Reserve OK — создаём Order со снимком цен.
            var drafts = request.Items.Select(i =>
            {
                var sku = skusById[i.SkuId];
                var productTitle = productsById.TryGetValue(sku.ProductId, out var p)
                    ? p.Title
                    : sku.Name;  // fallback: если продукт не пришёл, используем имя SKU

                return new OrderItemDraft(
                    SkuId: sku.Id,
                    ProductId: sku.ProductId,
                    ProductTitle: productTitle,
                    SkuName: sku.Name,
                    Quantity: i.Quantity,
                    UnitPrice: sku.Price);
            }).ToList();

            var order = Order.Create(
                buyerId,
                idempotencyKey,
                DeliveryAddress.Of(request.DeliveryAddress),
                drafts);

            // CREATED → PAID атомарно (mock-оплата, см. канон-flow).
            order.MarkAsPaid();

            await _orderRepository.AddAsync(order, ct);

            _logger.LogInformation(
                "Order {OrderId} created and paid for buyer {BuyerId}, total {Total}",
                order.Id, buyerId, order.TotalAmount);

            return OrdersMapper.ToDetailDto(order);
        }
    }
}
