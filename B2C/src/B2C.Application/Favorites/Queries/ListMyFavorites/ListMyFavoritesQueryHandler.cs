using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Application.Favorites.Dtos;
using B2C.Application.Integration;
using B2C.Domain.Favorites;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.Favorites.Queries.ListMyFavorites
{
    /// <summary>
    /// Шаги:
    ///   1. Загрузить весь Favorite-агрегат (нужны AddedAt + ProductId).
    ///   2. Извлечь список ProductId.
    ///   3. Batch-запрос в B2B: GetProductsBatchAsync.
    ///   4. Замапить в FavoriteListItemDto, объединив с AddedAt из Favorite.
    /// 
    /// КРИТИЧНО: товары могут быть НЕ найдены в B2B (удалены/заблокированы).
    /// В этом случае B2B вернёт меньше items, чем было запрошено.
    /// Наша политика: показываем только то, что вернул B2B. Удалённые товары
    /// "выпадают" из списка избранного (но из БД не удаляются — это сделает
    /// обработчик ProductDeleted, см. HandleProductDeletedCommandHandler).
    /// </summary>
    public sealed class ListMyFavoritesQueryHandler
        : IRequestHandler<ListMyFavoritesQuery, IReadOnlyList<FavoriteListItemDto>>
    {
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IB2BCatalogClient _b2bCatalog;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<ListMyFavoritesQueryHandler> _logger;

        public ListMyFavoritesQueryHandler(
            IFavoriteRepository favoriteRepository,
            IB2BCatalogClient b2bCatalog,
            ICurrentUserService currentUser,
            ILogger<ListMyFavoritesQueryHandler> logger)
        {
            _favoriteRepository = favoriteRepository;
            _b2bCatalog = b2bCatalog;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<IReadOnlyList<FavoriteListItemDto>> Handle(
            ListMyFavoritesQuery request, CancellationToken ct)
        {
            // Замечание: текущий IFavoriteRepository не возвращает Favorite целиком,
            // только ProductIds. Чтобы получить AddedAt, нужен метод ListByBuyerAsync,
            // возвращающий Favorite-агрегаты. Добавляю этот метод в порт ниже.
            var favorites = await _favoriteRepository.ListByBuyerAsync(_currentUser.BuyerId, ct);

            if (favorites.Count == 0)
                return Array.Empty<FavoriteListItemDto>();

            var productIds = favorites.Select(f => f.ProductId).ToList();
            var products = await _b2bCatalog.GetProductsBatchAsync(productIds, ct);

            // Индексируем продукты для O(1)-поиска при склейке с Favorite.AddedAt.
            var productsById = products.ToDictionary(p => p.Id);

            var result = new List<FavoriteListItemDto>(favorites.Count);
            foreach (var fav in favorites.OrderByDescending(f => f.CreatedAt))
            {
                if (productsById.TryGetValue(fav.ProductId, out var product))
                    result.Add(FavoritesMapper.ToListItem(product, fav.CreatedAt));
                else
                    _logger.LogWarning(
                        "Favorite {FavId} references product {ProductId} not found in B2B — skipping",
                        fav.Id, fav.ProductId);
            }

            return result;
        }
    }
}
