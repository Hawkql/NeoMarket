using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleSkuBackInStock
{
    public sealed class HandleSkuBackInStockCommandHandler
        : IRequestHandler<HandleSkuBackInStockCommand>
    {
        private const string EventType = "sku_back_in_stock_v1";
        private const string Source = "b2b";

        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ILogger<HandleSkuBackInStockCommandHandler> _logger;

        public HandleSkuBackInStockCommandHandler(
            IIdempotencyStore idempotencyStore,
            ILogger<HandleSkuBackInStockCommandHandler> logger)
        {
            _idempotencyStore = idempotencyStore;
            _logger = logger;
        }

        public async Task Handle(HandleSkuBackInStockCommand request, CancellationToken ct)
        {
            if (await _idempotencyStore.ExistsAsync(request.IdempotencyKey, ct))
            {
                _logger.LogInformation(
                    "SkuBackInStock event {Key} already processed — skipping",
                    request.IdempotencyKey);
                return;
            }

            _idempotencyStore.Register(
                request.IdempotencyKey,
                EventType,
                Source,
                JsonSerializer.Serialize(new
                {
                    request.ProductId,
                    request.SkuId,
                    request.AvailableQuantity,
                }));

            _logger.LogInformation(
                "SkuBackInStock {SkuId} qty={Qty}: notifications trigger TODO (Outbox)",
                request.SkuId, request.AvailableQuantity);

            // TODO: найти Subscriptions с notify_on содержащим BACK_IN_STOCK на этот product/sku
            //       и положить outbox-сообщения для notification service.
        }
    }
}