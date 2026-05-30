using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Carts;
using B2C.Domain.Favorites;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductDeleted
{
    /// <summary>
    /// Реакция B2C на ProductDeleted от B2B (US-ORD-04 / EVT-3).
    /// Отличие от ProductBlocked: товар удалён окончательно, поэтому
    /// дополнительно ЧИСТИМ Favorites (товара больше нет).
    /// </summary>
    public sealed class HandleProductDeletedCommandHandler : IRequestHandler<HandleProductDeletedCommand>
    {
        private const string EventType = "product_deleted_v1";
        private const string Source = "b2b";

        private readonly ICartRepository _cartRepository;
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ILogger<HandleProductDeletedCommandHandler> _logger;

        public HandleProductDeletedCommandHandler(
            ICartRepository cartRepository,
            IFavoriteRepository favoriteRepository,
            IIdempotencyStore idempotencyStore,
            ILogger<HandleProductDeletedCommandHandler> logger)
        {
            _cartRepository = cartRepository;
            _favoriteRepository = favoriteRepository;
            _idempotencyStore = idempotencyStore;
            _logger = logger;
        }

        public async Task Handle(HandleProductDeletedCommand request, CancellationToken ct)
        {
            if (await _idempotencyStore.ExistsAsync(request.IdempotencyKey, ct))
            {
                _logger.LogInformation(
                    "ProductDeleted event {Key} already processed — skipping",
                    request.IdempotencyKey);
                return;
            }

            // 1. Помечаем позиции корзин.
            var carts = await _cartRepository.ListWithProductAsync(request.ProductId, ct);
            var cartItemsAffected = 0;
            foreach (var cart in carts)
            {
                cartItemsAffected += cart.MarkProductUnavailable(
                    request.ProductId, UnavailableReason.ProductDeleted);
            }

            // 2. Bulk-удаление избранного — товара больше нет ни у кого.
            await _favoriteRepository.RemoveAllByProductAsync(request.ProductId, ct);

            _idempotencyStore.Register(
                request.IdempotencyKey,
                EventType,
                Source,
                JsonSerializer.Serialize(new { request.ProductId, request.SkuIds }));

            _logger.LogInformation(
                "ProductDeleted {ProductId}: {CartItems} cart items marked, favorites cleared",
                request.ProductId, cartItemsAffected);
        }
    }
}
