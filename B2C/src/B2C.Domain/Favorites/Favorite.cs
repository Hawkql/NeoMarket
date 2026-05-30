using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Common;
using B2C.Domain.Favorites.Events;

namespace B2C.Domain.Favorites
{
    public sealed class Favorite : AggregateRoot<Guid>, IAuditableEntity
    {
        public Guid BuyerId { get; private set; }
        public Guid ProductId { get; private set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Favorite() { }

        private Favorite(Guid id, Guid buyerId, Guid productId) : base(id)
        {
            BuyerId = buyerId;
            ProductId = productId;
        }

        public static Favorite Create(Guid buyerId, Guid productId)
        {
            if (buyerId == Guid.Empty)
                throw new DomainException("BuyerId is required", "INVALID_REQUEST");
            if (productId == Guid.Empty)
                throw new DomainException("ProductId is required", "INVALID_REQUEST");

            var favorite = new Favorite(Guid.NewGuid(), buyerId, productId);
            favorite.RaiseDomainEvent(new ProductAddedToFavoritesEvent(favorite.Id, buyerId, productId));
            return favorite;
        }
    }
}
