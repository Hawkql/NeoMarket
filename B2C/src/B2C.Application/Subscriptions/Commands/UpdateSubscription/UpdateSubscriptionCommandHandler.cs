using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Application.Subscriptions.Dtos;
using B2C.Domain.Common;
using B2C.Domain.Subscriptions;
using MediatR;

namespace B2C.Application.Subscriptions.Commands.UpdateSubscription
{
    /// <summary>
    /// Отличается от Subscribe тем, что НЕ создаёт подписку, если её нет — возвращает NOT_FOUND.
    /// Это правильная REST-семантика: PATCH несуществующего ресурса = 404.
    /// </summary>
    public sealed class UpdateSubscriptionCommandHandler : IRequestHandler<UpdateSubscriptionCommand>
    {
        private readonly IProductSubscriptionRepository _subscriptionRepository;
        private readonly ICurrentUserService _currentUser;

        public UpdateSubscriptionCommandHandler(
            IProductSubscriptionRepository subscriptionRepository,
            ICurrentUserService currentUser)
        {
            _subscriptionRepository = subscriptionRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(UpdateSubscriptionCommand request, CancellationToken ct)
        {
            var subscription = await _subscriptionRepository.GetAsync(
                _currentUser.BuyerId, request.ProductId, ct)
                ?? throw new DomainException("Subscription not found", "NOT_FOUND");

            subscription.ChangeNotifyOn(SubscriptionsMapper.ToDomain(request.NotifyOn));
        }
    }
}
