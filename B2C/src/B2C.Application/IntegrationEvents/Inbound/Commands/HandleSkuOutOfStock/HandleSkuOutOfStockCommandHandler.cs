using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Carts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleSkuOutOfStock
{
    /// <summary>
    /// Реакция B2C на SKU_OUT_OF_STOCK от B2B (US-ORD-04 / EVT-3).
    /// 
    /// Помечаем позиции корзин с конкретным sku_id (не product_id — другие SKU товара
    /// могут быть в наличии). Favorites и Orders не трогаются.
    /// 
    /// NB: обратное событие "SKU вернулся в наличие" контрактом EVT-3 не предусмотрено.
    /// Когда (и если) появится — будем снимать UnavailableReason и триггерить уведомления
    /// для Subscriptions с notify_on=IN_STOCK.
    /// </summary>
    public sealed class HandleSkuOutOfStockCommandHandler : IRequestHandler<HandleSkuOutOfStockCommand>
    {
        private const string EventType = "sku_out_of_stock_v1";
        private const string Source = "b2b";

        private readonly ICartRepository _cartRepository;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ILogger<HandleSkuOutOfStockCommandHandler> _logger;

        public HandleSkuOutOfStockCommandHandler(
            ICartRepository cartRepository,
            IIdempotencyStore idempotencyStore,
            ILogger<HandleSkuOutOfStockCommandHandler> logger)
        {
            _cartRepository = cartRepository;
            _idempotencyStore = idempotencyStore;
            _logger = logger;
        }

        public async Task Handle(HandleSkuOutOfStockCommand request, CancellationToken ct)
        {
            if (await _idempotencyStore.ExistsAsync(request.IdempotencyKey, ct))
            {
                _logger.LogInformation(
                    "SkuOutOfStock event {Key} already processed — skipping",
                    request.IdempotencyKey);
                return;
            }

            var carts = await _cartRepository.ListWithAnySkuAsync(new[] { request.SkuId }, ct);
            var totalAffected = 0;
            foreach (var cart in carts)
            {
                totalAffected += cart.MarkSkusUnavailable(
                    new[] { request.SkuId }, UnavailableReason.OutOfStock);
            }

            _idempotencyStore.Register(
                request.IdempotencyKey,
                EventType,
                Source,
                JsonSerializer.Serialize(new { request.ProductId, request.SkuId }));

            _logger.LogInformation(
                "SkuOutOfStock {SkuId}: marked {Affected} cart items",
                request.SkuId, totalAffected);
        }
    }
}
