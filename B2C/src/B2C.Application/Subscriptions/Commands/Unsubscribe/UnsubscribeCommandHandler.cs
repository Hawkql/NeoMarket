using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Subscriptions;
using MediatR;

namespace B2C.Application.Subscriptions.Commands.Unsubscribe
{
    /// <summary>
    /// Идемпотентно: нет подписки — успешный 204 без ошибки.
    /// </summary>
    public sealed class UnsubscribeCommandHandler : IRequestHandler<UnsubscribeCommand>
    {
        private readonly IProductSubscriptionRepository _subscriptionRepository;
        private readonly ICurrentUserService _currentUser;

        public UnsubscribeCommandHandler(
            IProductSubscriptionRepository subscriptionRepository,
            ICurrentUserService currentUser)
        {
            _subscriptionRepository = subscriptionRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(UnsubscribeCommand request, CancellationToken ct)
        {
            var subscription = await _subscriptionRepository.GetAsync(
                _currentUser.BuyerId, request.ProductId, ct);

            if (subscription is null) return;  // idempotent

            _subscriptionRepository.Remove(subscription);
        }
    }
}
