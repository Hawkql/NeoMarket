using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandlePriceChanged
{
    public sealed class HandlePriceChangedCommandHandler
        : IRequestHandler<HandlePriceChangedCommand>
    {
        private const string EventType = "price_changed_v1";
        private const string Source = "b2b";

        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ILogger<HandlePriceChangedCommandHandler> _logger;

        public HandlePriceChangedCommandHandler(
            IIdempotencyStore idempotencyStore,
            ILogger<HandlePriceChangedCommandHandler> logger)
        {
            _idempotencyStore = idempotencyStore;
            _logger = logger;
        }

        public async Task Handle(HandlePriceChangedCommand request, CancellationToken ct)
        {
            if (await _idempotencyStore.ExistsAsync(request.IdempotencyKey, ct))
            {
                _logger.LogInformation(
                    "PriceChanged event {Key} already processed — skipping",
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
                    request.OldPrice,
                    request.NewPrice,
                }));

            var priceDrop = request.NewPrice < request.OldPrice;
            _logger.LogInformation(
                "PriceChanged {SkuId}: {Old} -> {New} (drop={Drop}): cache invalidation + notifications TODO",
                request.SkuId, request.OldPrice, request.NewPrice, priceDrop);

            // TODO: catalog cache invalidation; trigger PRICE_DROP notifications.
        }
    }
}