using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Carts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductHardBlocked
{
    public sealed class HandleProductHardBlockedCommandHandler
        : IRequestHandler<HandleProductHardBlockedCommand>
    {
        private const string EventType = "product_hard_blocked_v1";
        private const string Source = "b2b";

        private readonly ICartRepository _cartRepository;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ILogger<HandleProductHardBlockedCommandHandler> _logger;

        public HandleProductHardBlockedCommandHandler(
            ICartRepository cartRepository,
            IIdempotencyStore idempotencyStore,
            ILogger<HandleProductHardBlockedCommandHandler> logger)
        {
            _cartRepository = cartRepository;
            _idempotencyStore = idempotencyStore;
            _logger = logger;
        }

        public async Task Handle(HandleProductHardBlockedCommand request, CancellationToken ct)
        {
            if (await _idempotencyStore.ExistsAsync(request.IdempotencyKey, ct))
            {
                _logger.LogInformation(
                    "ProductHardBlocked event {Key} already processed — skipping",
                    request.IdempotencyKey);
                return;
            }

            var carts = await _cartRepository.ListWithProductAsync(request.ProductId, ct);
            var totalAffected = 0;
            foreach (var cart in carts)
            {
                totalAffected += cart.MarkProductUnavailable(
                    request.ProductId, UnavailableReason.ProductBlocked);
            }

            _idempotencyStore.Register(
                request.IdempotencyKey,
                EventType,
                Source,
                JsonSerializer.Serialize(new { request.ProductId, request.Reason }));

            _logger.LogInformation(
                "ProductHardBlocked {ProductId}: marked {Affected} cart items (reason: {Reason})",
                request.ProductId, totalAffected, request.Reason ?? "n/a");
        }
    }
}