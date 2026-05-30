using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Favorites;
using MediatR;

namespace B2C.Application.Favorites.Commands.RemoveFavorite
{
    /// <summary>
    /// Идемпотентно: если такого Favorite нет — возвращаем успех (без ошибки).
    /// Это согласовано с DELETE-семантикой HTTP: DELETE должен быть идемпотентен.
    /// </summary>
    public sealed class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand>
    {
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly ICurrentUserService _currentUser;

        public RemoveFavoriteCommandHandler(
            IFavoriteRepository favoriteRepository,
            ICurrentUserService currentUser)
        {
            _favoriteRepository = favoriteRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(RemoveFavoriteCommand request, CancellationToken ct)
        {
            var favorite = await _favoriteRepository.GetAsync(
                _currentUser.BuyerId, request.ProductId, ct);

            if (favorite is null) return;  // идемпотентно

            _favoriteRepository.Remove(favorite);
        }
    }
}
