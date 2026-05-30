using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Application.Integration;
using B2C.Application.Subscriptions.Dtos;
using B2C.Domain.Subscriptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.Subscriptions.Queries.ListMySubscriptions
{
    /// <summary>
    /// Batch enrichment (как в Favorites):
    ///   1. Загружаем подписки покупателя.
    ///   2. Одним запросом обогащаем данными о товаре из B2B.
    ///   3. Склейка через словарь по ProductId.
    /// 
    /// Если товар не найден в B2B (удалён) — подписка не выводится в списке.
    /// Cleanup таких подписок — отдельная задача (можно сделать в HandleProductDeleted-handler,
    /// сейчас там это не делается; можно добавить).
    /// </summary>
    public sealed class ListMySubscriptionsQueryHandler
        : IRequestHandler<ListMySubscriptionsQuery, IReadOnlyList<SubscriptionDto>>
    {
        private readonly IProductSubscriptionRepository _subscriptionRepository;
        private readonly IB2BCatalogClient _b2bCatalog;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<ListMySubscriptionsQueryHandler> _logger;

        public ListMySubscriptionsQueryHandler(
            IProductSubscriptionRepository subscriptionRepository,
            IB2BCatalogClient b2bCatalog,
            ICurrentUserService currentUser,
            ILogger<ListMySubscriptionsQueryHandler> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _b2bCatalog = b2bCatalog;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<IReadOnlyList<SubscriptionDto>> Handle(
            ListMySubscriptionsQuery request, CancellationToken ct)
        {
            var subscriptions = await _subscriptionRepository.ListByBuyerAsync(_currentUser.BuyerId, ct);

            if (subscriptions.Count == 0)
                return Array.Empty<SubscriptionDto>();

            var productIds = subscriptions.Select(s => s.ProductId).ToList();
            var products = await _b2bCatalog.GetProductsBatchAsync(productIds, ct);
            var productsById = products.ToDictionary(p => p.Id);

            var result = new List<SubscriptionDto>(subscriptions.Count);
            foreach (var sub in subscriptions.OrderByDescending(s => s.CreatedAt))
            {
                if (productsById.TryGetValue(sub.ProductId, out var product))
                    result.Add(SubscriptionsMapper.ToDto(sub, product));
                else
                    _logger.LogWarning(
                        "Subscription {SubId} references product {ProductId} not found in B2B — skipping",
                        sub.Id, sub.ProductId);
            }

            return result;
        }
    }
}
