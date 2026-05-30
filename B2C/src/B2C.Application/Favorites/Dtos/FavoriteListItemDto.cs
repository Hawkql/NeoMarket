using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Favorites.Dtos
{
    /// <summary>
    /// Карточка товара в списке избранного. Похожа на CatalogProductCardDto,
    /// но добавлено поле AddedAt (когда покупатель добавил в избранное).
    /// </summary>
    public sealed record FavoriteListItemDto(
        Guid ProductId,
        string Title,
        string? ImageUrl,
        int Price,
        int? OldPrice,
        int? Discount,
        bool InStock,
        double? Rating,
        int? ReviewsCount,
        DateTime AddedAt);
}
