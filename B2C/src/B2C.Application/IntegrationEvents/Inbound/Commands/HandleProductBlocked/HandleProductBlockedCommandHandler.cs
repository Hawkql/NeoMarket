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

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductBlocked
{
    /// <summary>
    /// Реакция B2C на ProductBlocked от B2B (US-ORD-04 / EVT-3).
    /// 
    /// Шаги:
    ///   1. Inbox-дедупликация по IdempotencyKey — повторный вызов = no-op.
    ///   2. Найти все корзины с этим product_id.
    ///   3. Каждой пометить позиции с этим product_id как UnavailableReason.ProductBlocked.
    ///   4. Зарегистрировать ключ в Inbox.
    ///   5. SaveChanges (вызовется TransactionBehavior).
    /// 
    /// Что НЕ делается (намеренно):
    ///   - Favorites НЕ удаляются (покупатель сам увидит "недоступно");
    ///   - Orders НЕ меняются (даже если статус Created — продавец должен отгрузить уже оплаченные).
    /// </summary>
    public sealed class HandleProductBlockedCommandHandler : IRequestHandler<HandleProductBlockedCommand>
    {
        private const string EventType = "product_blocked_v1";
        private const string Source = "b2b";

        private readonly ICartRepository _cartRepository;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ILogger<HandleProductBlockedCommandHandler> _logger;

        public HandleProductBlockedCommandHandler(
            ICartRepository cartRepository,
            IIdempotencyStore idempotencyStore,
            ILogger<HandleProductBlockedCommandHandler> logger)
        {
            _cartRepository = cartRepository;
            _idempotencyStore = idempotencyStore;
            _logger = logger;
        }

        public async Task Handle(HandleProductBlockedCommand request, CancellationToken ct)
        {
            // 1. Inbox check — уже обрабатывали?
            if (await _idempotencyStore.ExistsAsync(request.IdempotencyKey, ct))
            {
                _logger.LogInformation(
                    "ProductBlocked event {Key} already processed — skipping",
                    request.IdempotencyKey);
                return;
            }

            // 2. Найти все корзины с этим товаром (cross-buyer query).
            var affectedCarts = await _cartRepository.ListWithProductAsync(request.ProductId, ct);

            // 3. Пометить позиции.
            var totalAffected = 0;
            foreach (var cart in affectedCarts)
            {
                totalAffected += cart.MarkProductUnavailable(
                    request.ProductId, UnavailableReason.ProductBlocked);
            }

            // 4. Зарегистрировать ключ в Inbox (запись пройдёт в той же транзакции).
            _idempotencyStore.Register(
                request.IdempotencyKey,
                EventType,
                Source,
                JsonSerializer.Serialize(new { request.ProductId, request.SkuIds }));

            _logger.LogInformation(
                "ProductBlocked {ProductId}: marked {Affected} cart items in {CartCount} carts",
                request.ProductId, totalAffected, affectedCarts.Count);

            // 5. SaveChanges делает TransactionBehavior.
        }
    }
}
