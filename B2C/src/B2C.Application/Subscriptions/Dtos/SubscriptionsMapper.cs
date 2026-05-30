using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration.Dtos;
using B2C.Domain.Subscriptions;

namespace B2C.Application.Subscriptions.Dtos
{
    internal static class SubscriptionsMapper
    {
        /// <summary>
        /// Domain.NotifyOn → API NotifyOnDto. Один-в-один маппинг битовых флагов,
        /// но через explicit switch — чтобы при добавлении нового значения в Domain
        /// компилятор подсказал, что нужно расширить и API.
        /// </summary>
        public static NotifyOnDto ToDto(NotifyOn domain)
        {
            var result = NotifyOnDto.None;
            if (domain.HasFlag(NotifyOn.InStock)) result |= NotifyOnDto.InStock;
            if (domain.HasFlag(NotifyOn.PriceDrop)) result |= NotifyOnDto.PriceDrop;
            return result;
        }

        /// <summary>API NotifyOnDto → Domain.NotifyOn. Обратный маппинг.</summary>
        public static NotifyOn ToDomain(NotifyOnDto dto)
        {
            var result = NotifyOn.None;
            if (dto.HasFlag(NotifyOnDto.InStock)) result |= NotifyOn.InStock;
            if (dto.HasFlag(NotifyOnDto.PriceDrop)) result |= NotifyOn.PriceDrop;
            return result;
        }

        /// <summary>
        /// Обогащённая подписка: Domain + B2B-данные о товаре.
        /// </summary>
        public static SubscriptionDto ToDto(ProductSubscription sub, ProductSummary product) =>
            new(sub.Id,
                sub.ProductId,
                product.Title,
                product.ImageUrl,
                product.Price,
                product.InStock,
                ToDto(sub.NotifyOn),
                sub.CreatedAt);
    }
}
