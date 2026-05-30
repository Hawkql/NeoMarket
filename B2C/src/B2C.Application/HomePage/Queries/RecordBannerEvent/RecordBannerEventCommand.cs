using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.HomePage;
using MediatR;

namespace B2C.Application.HomePage.Queries.RecordBannerEvent
{
    /// <summary>
    /// Фиксация события impression/click для CTR-аналитики (US-CART-04).
    /// 
    /// BuyerId опциональный — для гостей null (impression от анонимного пользователя).
    /// SessionId опциональный — для cookie-less трафика тоже null.
    /// Контроллер сам определит, кто инициатор (JWT, X-Session-Id, или анонимно).
    /// 
    /// Тип события передаётся доменным enum'ом BannerEventType (Impression/Click)
    /// напрямую — это простой enum без API-специфичных переименований.
    /// </summary>
    public sealed record RecordBannerEventCommand(
        Guid BannerId,
        BannerEventType Type,
        Guid? BuyerId,
        string? SessionId) : IRequest;
}
