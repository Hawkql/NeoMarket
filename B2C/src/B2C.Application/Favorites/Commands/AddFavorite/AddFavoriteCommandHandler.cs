using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Favorites;
using MediatR;

namespace B2C.Application.Favorites.Commands.AddFavorite
{

    /// <summary>
    /// Идемпотентность: проверяем, есть ли уже Favorite с этой парой (BuyerId, ProductId).
    /// Если есть — no-op, возвращаем успех. Иначе создаём новый.
    /// 
    /// Альтернатива: полагаться на unique-index в БД и ловить конфликт.
    /// Я выбрал явную проверку — это N+1 операций в худшем случае, но:
    /// 1. Простота: нет ловли DbException и его трансляции
    /// 2. Защита от race: даже при race-condition unique-index в БД остановит, 
    ///    SaveChanges бросит — Application можно перехватить (но это редкий случай).
    /// </summary>
    public sealed class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand>
    {
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly ICurrentUserService _currentUser;

        public AddFavoriteCommandHandler(
            IFavoriteRepository favoriteRepository,
            ICurrentUserService currentUser)
        {
            _favoriteRepository = favoriteRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(AddFavoriteCommand request, CancellationToken ct)
        {
            var buyerId = _currentUser.BuyerId;

            if (await _favoriteRepository.ExistsAsync(buyerId, request.ProductId, ct))
                return;  // идемпотентность

            var favorite = Favorite.Create(buyerId, request.ProductId);
            await _favoriteRepository.AddAsync(favorite, ct);
        }
    }
}
