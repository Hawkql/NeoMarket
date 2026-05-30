using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Favorites.Dtos;
using MediatR;

namespace B2C.Application.Favorites.Queries.ListMyFavorites
{
    public sealed record ListMyFavoritesQuery : IRequest<IReadOnlyList<FavoriteListItemDto>>;
}
