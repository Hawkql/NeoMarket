using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// US-CART-04 строгое:
    ///   - Если баннер не существует → 400 BANNER_NOT_FOUND.
    ///   - Иначе запись CTR-события через BannerEvent.Record + персист.
    /// 
    /// Write-only операция: ничего не возвращаем, контроллер отдаст 204.
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

            // 1. Проверка существования товара в B2B (US-CART-02 acceptance).
            //    Batch на один ID — без отдельного метода ExistsAsync на клиенте.
            var products = await _catalogClient.GetProductsBatchAsync(
                new[] { request.ProductId }, ct);

            if (!products.Any(p => p.Id == request.ProductId))
                throw new DomainException(
                    $"Product {request.ProductId} not found in catalog", "PRODUCT_NOT_FOUND");

            // 2. Проверка дубля (US-CART-02 acceptance).
            var existing = await _subscriptionRepository.GetAsync(buyerId, request.ProductId, ct);
            if (existing is not null)
                throw new DomainException(
                    "Already subscribed to this product", "ALREADY_SUBSCRIBED");

            // 3. Создание.
            var notifyOn = SubscriptionsMapper.ToDomain(request.NotifyOn);
            var subscription = ProductSubscription.Create(buyerId, request.ProductId, notifyOn);
            await _subscriptionRepository.AddAsync(subscription, ct);
        }
    }
}
