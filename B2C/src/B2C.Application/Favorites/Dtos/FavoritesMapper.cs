using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration.Dtos;


namespace B2C.Application.Favorites.Dtos
{
    internal static class FavoritesMapper
    {
        /// <summary>
        /// Обогащение Favorite + ProductSummary → FavoriteListItemDto.
        /// AddedAt берётся из Favorite (наш домен), остальное — из B2B.
        /// </summary>
        public static FavoriteListItemDto ToListItem(ProductSummary p, System.DateTime addedAt) =>
            new(p.Id,
                p.Title,
                p.ImageUrl,
                p.Price,
                p.OldPrice,
                p.Discount,
                p.InStock,
                p.Rating,
                p.ReviewsCount,
                addedAt);
    }
}
