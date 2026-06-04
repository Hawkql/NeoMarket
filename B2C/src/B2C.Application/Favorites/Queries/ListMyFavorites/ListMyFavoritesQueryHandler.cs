using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Common.Abstractions;
using B2C.Application.Common.Pagination;
using B2C.Application.Integration;
using B2C.Domain.Favorites;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.Favorites.Queries.ListMyFavorites
{
    /// <summary>
    /// openapi: возвращаем PaginatedCatalogProducts — тот же формат, что каталог.
    /// AddedAt не отдаём (поля нет в CatalogProductCard).
    /// Порядок: новые сверху (OrderByDescending CreatedAt) — куратор UX.
    /// </summary>
    public sealed class ListMyFavoritesQueryHandler
        : IRequestHandler<ListMyFavoritesQuery, PagedResult<CatalogProductCardDto>>
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

        public async Task<PagedResult<CatalogProductCardDto>> Handle(
            ListMyFavoritesQuery request, CancellationToken ct)
        {
            var buyerId = _currentUser.BuyerId;

            // 1. Все Favorite покупателя (для total_count и сортировки).
            var favorites = await _favoriteRepository.ListByBuyerAsync(buyerId, ct);
            var totalCount = favorites.Count;

            if (totalCount == 0)
                return new PagedResult<CatalogProductCardDto>(
                    Array.Empty<CatalogProductCardDto>(), 0, request.Limit, request.Offset);

            // 2. Сортировка и пагинация — на нашей стороне (в БД нет JOIN с B2B).
            var pageFavorites = favorites
                .OrderByDescending(f => f.CreatedAt)
                .Skip(request.Offset)
                .Take(request.Limit)
                .ToList();

            if (pageFavorites.Count == 0)
                return new PagedResult<CatalogProductCardDto>(
                    Array.Empty<CatalogProductCardDto>(), totalCount, request.Limit, request.Offset);

            // 3. Batch enrichment, маппинг в CatalogProductCardDto.
            var productIds = pageFavorites.Select(f => f.ProductId).ToList();
            var products = await _b2bCatalog.GetProductsBatchAsync(productIds, ct);
            var productsById = products.ToDictionary(p => p.Id);

            var items = new List<CatalogProductCardDto>(pageFavorites.Count);
            foreach (var fav in pageFavorites)
            {
                if (productsById.TryGetValue(fav.ProductId, out var product))
                    items.Add(CatalogMapper.ToCard(product));
                else
                    _logger.LogWarning(
                        "Favorite {FavId} references product {ProductId} not found in B2B — skipping",
                        fav.Id, fav.ProductId);
            }

            return new PagedResult<CatalogProductCardDto>(
                items, totalCount, request.Limit, request.Offset);
        }
    }
}