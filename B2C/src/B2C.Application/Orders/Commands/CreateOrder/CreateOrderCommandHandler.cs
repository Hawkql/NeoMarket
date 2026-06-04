using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;
using B2C.Application.Orders.Dtos;
using B2C.Domain.Addresses;
using B2C.Domain.Common;
using B2C.Domain.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.Orders.Commands.CreateOrder
{
    public sealed class CreateOrderCommandHandler
        : IRequestHandler<CreateOrderCommand, OrderResponseDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IAddressRepository _addressRepository;
        private readonly IB2BCatalogClient _b2bCatalog;
        private readonly IB2BReservationClient _b2bReservation;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(
            IOrderRepository orderRepository,
            IAddressRepository addressRepository,
            IB2BCatalogClient b2bCatalog,
            IB2BReservationClient b2bReservation,
            ICurrentUserService currentUser,
            ILogger<CreateOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _addressRepository = addressRepository;
            _b2bCatalog = b2bCatalog;
            _b2bReservation = b2bReservation;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<OrderResponseDto> Handle(CreateOrderCommand request, CancellationToken ct)
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
                return OrdersMapper.ToResponseDto(existing);
            }

            // 2. IDOR-проверка адреса: только адрес ЭТОГО buyer'а.
            //    Чужой/несуществующий → NOT_FOUND (не FORBIDDEN — не раскрываем существование).
            var address = await _addressRepository.GetByIdForBuyerAsync(request.AddressId, buyerId, ct)
                ?? throw new DomainException(
                    "Address not found", "NOT_FOUND");

            // 3. Snapshot из B2B: цены + ProductId + названия.
            var skuIds = request.Items.Select(i => i.SkuId).Distinct().ToList();
            var skus = await _b2bCatalog.GetSkusBatchAsync(skuIds, ct);
            var skusById = skus.ToDictionary(s => s.Id);

            var missing = skuIds.Where(id => !skusById.ContainsKey(id)).ToList();
            if (missing.Any())
                throw new DomainException(
                    $"SKUs not found in catalog: {string.Join(", ", missing)}", "INVALID_REQUEST");

            var productIds = skus.Select(s => s.ProductId).Distinct().ToList();
            var products = await _b2bCatalog.GetProductsBatchAsync(productIds, ct);
            var productsById = products.ToDictionary(p => p.Id);

            // 4. Reserve в B2B (до создания Order).
            var reserveLines = request.Items
                .Select(i => new ReserveLine(i.SkuId, i.Quantity))
                .ToList();

            var reserveResult = await _b2bReservation.ReserveAsync(
                request.IdempotencyKey, reserveLines, ct);

            if (!reserveResult.Success)
            {
                _logger.LogWarning(
                    "Reserve failed for buyer {BuyerId}, key {Key}, {Count} failed items",
                    buyerId, request.IdempotencyKey, reserveResult.FailedItems.Count);

                var failedItems = reserveResult.FailedItems
                    .Select(f => new
                    {
                        sku_id = f.SkuId,
                        requested = f.Requested,
                        available = f.Available,
                        reason = f.Reason.ToString().ToLowerInvariant(),
                    })
                    .ToList();

                throw new DomainException(
                    "Cannot reserve some items",
                    "RESERVE_FAILED",
                    details: new { failed_items = failedItems });
            }

            // 5. Drafts: snapshot цен и названий на момент создания.
            var drafts = request.Items.Select(i =>
            {
                var sku = skusById[i.SkuId];
                var productTitle = productsById.TryGetValue(sku.ProductId, out var p)
                    ? p.Title
                    : sku.Name;

                return new OrderItemDraft(
                    SkuId: sku.Id,
                    ProductId: sku.ProductId,
                    ProductTitle: productTitle,
                    SkuName: sku.Name,
                    Quantity: i.Quantity,
                    UnitPrice: sku.Price);
            }).ToList();

            // 6. Snapshot адреса в Order (если Address у покупателя позже удалится — заказ сохранит).
            var addressSnapshot = new OrderAddress(
                originalAddressId: address.Id,
                country: address.Country,
                city: address.City,
                street: address.Street,
                house: address.House,
                apartment: address.Apartment,
                postalCode: address.PostalCode);

            var order = Order.Create(
                buyerId,
                idempotencyKey,
                addressSnapshot,
                request.PaymentMethodId,
                request.Comment,
                drafts);

            // CREATED → PAID атомарно (mock-оплата, см. canon-flow).
            order.MarkAsPaid();

            await _orderRepository.AddAsync(order, ct);

            _logger.LogInformation(
                "Order {OrderId} created and paid for buyer {BuyerId}, total {Total}",
                order.Id, buyerId, order.Total);

            return OrdersMapper.ToResponseDto(order);
        }
    }
}