using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Favorites.Commands.AddFavorite
{
    /// <summary>
    /// Добавление товара в избранное.
    /// BuyerId — из ICurrentUserService (IDOR-защита).
    /// Идемпотентно: повторное добавление того же товара — 200 OK, не CONFLICT
    /// (это лучше UX: фронт может смело отправлять POST без проверки exists).
    /// </summary>
    public sealed record AddFavoriteCommand(Guid ProductId) : IRequest;
}
