using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;

namespace B2C.Domain.Favorites.Events
{
    public record ProductAddedToFavoritesEvent(Guid FavoriteId, Guid BuyerId, Guid ProductId) : DomainEvent;
}
