using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Carts;
using B2C.Domain.Common;

namespace B2C.Application.Cart.Services
{
    /// <summary>
    /// Реализация ICartContextResolver. Лежит в Application, потому что не зависит от
    /// EF Core / HttpContext — только от Application-абстракций (ICurrentUserService, ISessionContext)
    /// и Domain (ICartRepository).
    /// </summary>
    public sealed class CartContextResolver : ICartContextResolver
    {
        private readonly ICurrentUserService _currentUser;
        private readonly ISessionContext _session;
        private readonly ICartRepository _cartRepository;

        public CartContextResolver(
            ICurrentUserService currentUser,
            ISessionContext session,
            ICartRepository cartRepository)
        {
            _currentUser = currentUser;
            _session = session;
            _cartRepository = cartRepository;
        }

        public async Task<Domain.Carts.Cart> GetOrCreateMyCartAsync(CancellationToken ct)
        {
            if (_currentUser.IsAuthenticated)
            {
                var existing = await _cartRepository.GetByBuyerAsync(_currentUser.BuyerId, ct);
                if (existing is not null) return existing;

                var newCart = Domain.Carts.Cart.ForBuyer(_currentUser.BuyerId);
                await _cartRepository.AddAsync(newCart, ct);
                return newCart;
            }

            if (_session.HasSession)
            {
                var existing = await _cartRepository.GetBySessionAsync(_session.SessionId!, ct);
                if (existing is not null) return existing;

                var newCart = Domain.Carts.Cart.ForGuest(_session.SessionId!);
                await _cartRepository.AddAsync(newCart, ct);
                return newCart;
            }

            throw new DomainException(
                "Either authentication or X-Session-Id is required to access cart",
                "UNAUTHORIZED");
        }

        public async Task<Domain.Carts.Cart?> GetMyCartOrNullAsync(CancellationToken ct)
        {
            if (_currentUser.IsAuthenticated)
                return await _cartRepository.GetByBuyerAsync(_currentUser.BuyerId, ct);

            if (_session.HasSession)
                return await _cartRepository.GetBySessionAsync(_session.SessionId!, ct);

            throw new DomainException(
                "Either authentication or X-Session-Id is required to access cart",
                "UNAUTHORIZED");
        }
    }
}
