using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Application.Integration;
using B2C.Application.Subscriptions.Dtos;
using B2C.Domain.Common;
using B2C.Domain.Subscriptions;
using MediatR;

namespace B2C.Application.Subscriptions.Commands.Subscribe
{
    /// <summary>
    /// openapi: POST /subscribe всегда 204 — upsert-семантика.
    /// 1. Проверка существования товара в B2B (US-CART-02 acceptance).
    /// 2. Если подписка уже есть — ChangeNotifyOn (перезаписываем), не бросаем 409.
    /// 3. Иначе создаём новую.
    /// </summary>
    public sealed class SubscribeCommandHandler : IRequestHandler<SubscribeCommand>
    {
        private readonly IProductSubscriptionRepository _subscriptionRepository;
        private readonly IB2BCatalogClient _catalogClient;
        private readonly ICurrentUserService _currentUser;

        public SubscribeCommandHandler(
            IProductSubscriptionRepository subscriptionRepository,
            IB2BCatalogClient catalogClient,
            ICurrentUserService currentUser)
        {
            _subscriptionRepository = subscriptionRepository;
            _catalogClient = catalogClient;
            _currentUser = currentUser;
        }

        public async Task Handle(SubscribeCommand request, CancellationToken ct)
        {
            var buyerId = _currentUser.BuyerId;

            // 1. Товар существует в B2B?
            var products = await _catalogClient.GetProductsBatchAsync(
                new[] { request.ProductId }, ct);
            if (!products.Any(p => p.Id == request.ProductId))
                throw new DomainException(
                    $"Product {request.ProductId} not found in catalog", "PRODUCT_NOT_FOUND");

            if (await _subscriptionRepository.GetAsync(buyerId, request.ProductId, ct) is not null)
                throw new DomainException(
                    "Already subscribed to this product", "ALREADY_SUBSCRIBED");

            var notifyOn = SubscriptionsMapper.ToDomain(request.NotifyOn);

            // 2. Upsert: если есть — обновляем NotifyOn, не создаём дубль.
            var existing = await _subscriptionRepository.GetAsync(buyerId, request.ProductId, ct);
            if (existing is not null)
            {
                existing.ChangeNotifyOn(notifyOn);
                return;
            }

            // 3. Создаём новую.
            var subscription = ProductSubscription.Create(buyerId, request.ProductId, notifyOn);
            await _subscriptionRepository.AddAsync(subscription, ct);
        }
    }
}