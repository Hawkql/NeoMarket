using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Domain.Carts.Events;
using B2C.Domain.Common;

namespace B2C.Domain.Carts
{
    public sealed class Cart : AggregateRoot<Guid>, IAuditableEntity
    {
        private readonly List<CartItem> _items = new();

        public CartOwner Owner { get; private set; } = null!;
        public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private Cart() { }

        private Cart(Guid id, CartOwner owner) : base(id)
        {
            Owner = owner;
        }

        public static Cart ForBuyer(Guid buyerId)
            => new(Guid.NewGuid(), CartOwner.ForBuyer(buyerId));

        public static Cart ForGuest(string sessionId)
            => new(Guid.NewGuid(), CartOwner.ForGuest(sessionId));

        /// <summary>
        /// Добавить SKU в корзину. Если SKU уже есть — увеличить quantity (idempotent add).
        /// US-CART-03: add_sku_increments_quantity_if_already_in_cart.
        /// </summary>
        public void AddItem(Guid skuId, Guid productId, int quantity)
        {
            if (skuId == Guid.Empty)
                throw new DomainException("SkuId is required", "INVALID_REQUEST");
            if (productId == Guid.Empty)
                throw new DomainException("ProductId is required", "INVALID_REQUEST");
            if (quantity < 1)
                throw new DomainException("Quantity must be >= 1", "INVALID_REQUEST");

            var existing = _items.FirstOrDefault(i => i.SkuId == skuId);
            if (existing is not null)
            {
                existing.IncreaseQuantity(quantity);
                // Если позиция раньше была помечена недоступной, а покупатель пытается её
                // снова добавить — оставляем reason, фронт сам решит, что показывать.
                // Снять reason может только событие от B2B (PRODUCT_RESTORED — не в scope MVP).
                return;
            }

            _items.Add(new CartItem(Guid.NewGuid(), Id, skuId, productId, quantity));
        }

        /// <summary>
        /// Заменить количество для SKU. Если SKU нет в корзине — NOT_FOUND.
        /// </summary>
        public void SetItemQuantity(Guid skuId, int quantity)
        {
            var item = _items.FirstOrDefault(i => i.SkuId == skuId)
                ?? throw new DomainException("SKU not in cart", "NOT_FOUND");
            item.SetQuantity(quantity);
        }

        /// <summary>
        /// Удалить SKU из корзины. Идемпотентно: если SKU нет — молча выходим.
        /// </summary>
        public void RemoveItem(Guid skuId)
        {
            var item = _items.FirstOrDefault(i => i.SkuId == skuId);
            if (item is null) return;
            _items.Remove(item);
        }

        /// <summary>
        /// Пометить позиции с указанными SkuId как недоступные.
        /// Используется реакцией на события от B2B (US-ORD-04, EVT-3).
        /// 
        /// Возвращает количество затронутых позиций — нужно вызывающему,
        /// чтобы решать, поднимать ли событие/писать ли в лог.
        /// </summary>
        public int MarkSkusUnavailable(IEnumerable<Guid> skuIds, UnavailableReason reason)
        {
            if (skuIds is null)
                throw new DomainException("skuIds is required", "INVALID_REQUEST");
            if (reason == UnavailableReason.None)
                throw new DomainException("Reason must be specified", "INVALID_REQUEST");

            var idSet = new HashSet<Guid>(skuIds);
            var affected = 0;
            foreach (var item in _items.Where(i => idSet.Contains(i.SkuId)))
            {
                item.MarkUnavailable(reason);
                affected++;
            }
            return affected;
        }

        /// <summary>
        /// Пометить ВСЕ позиции с указанным ProductId как недоступные.
        /// Событие PRODUCT_BLOCKED/PRODUCT_DELETED приходит с product_id (и опционально sku_ids).
        /// </summary>
        public int MarkProductUnavailable(Guid productId, UnavailableReason reason)
        {
            if (productId == Guid.Empty)
                throw new DomainException("ProductId is required", "INVALID_REQUEST");
            if (reason == UnavailableReason.None)
                throw new DomainException("Reason must be specified", "INVALID_REQUEST");

            var affected = 0;
            foreach (var item in _items.Where(i => i.ProductId == productId))
            {
                item.MarkUnavailable(reason);
                affected++;
            }
            return affected;
        }

        /// <summary>
        /// Слияние гостевой корзины в эту (US-CART-03: guest_cart_merged_on_login).
        /// При конфликте по SkuId берётся MAX(this.quantity, other.quantity).
        /// 
        /// Другая корзина остаётся нетронутой (она будет удалена в Application/Infrastructure
        /// после успешного merge — обычно в той же транзакции).
        /// </summary>
        public void Merge(Cart other)
        {
            if (other is null)
                throw new DomainException("Other cart is required", "INVALID_REQUEST");
            if (other.Id == Id)
                throw new DomainException("Cannot merge cart into itself", "INVALID_REQUEST");

            var mergedCount = 0;
            foreach (var src in other._items)
            {
                var existing = _items.FirstOrDefault(i => i.SkuId == src.SkuId);
                if (existing is null)
                {
                    _items.Add(new CartItem(Guid.NewGuid(), Id, src.SkuId, src.ProductId, src.Quantity));
                }
                else
                {
                    var max = Math.Max(existing.Quantity, src.Quantity);
                    if (max > existing.Quantity)
                        existing.SetQuantity(max);
                }
                mergedCount++;
            }

            if (mergedCount > 0)
                RaiseDomainEvent(new CartMergedEvent(Id, other.Id, mergedCount));
        }

        /// <summary>
        /// Полная очистка корзины. Вызывается фронтом после успешного checkout.
        /// </summary>
        public void Clear() => _items.Clear();
    }
}
