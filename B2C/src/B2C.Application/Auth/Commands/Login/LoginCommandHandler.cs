using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Auth.Dtos;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Buyers;
using B2C.Domain.Carts;
using B2C.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace B2C.Application.Auth.Commands.Login
{
    /// <summary>
    /// Шаги:
    ///   1. Найти покупателя по email. Не найден → UNAUTHORIZED (не CONFLICT — чтобы
    ///      не палить, какие email зарегистрированы; защита от user enumeration).
    ///   2. Verify пароль. Не совпал → UNAUTHORIZED.
    ///   3. Проверить, что не удалён.
    ///   4. Выдать токены, сохранить RefreshToken.
    ///   5. Merge гостевой корзины (US-CART-03), если SessionId передан:
    ///      - найти гостевую корзину;
    ///      - получить/создать пользовательскую;
    ///      - cart.Merge(guest) — domain делает MAX-merge per SkuId;
    ///      - удалить гостевую.
    /// 
    /// Merge — в этой же транзакции с выдачей токенов: либо всё, либо ничего.
    /// Если merge упадёт — пользователь увидит 500 и сможет повторить (idempotent
    /// благодаря тому, что после merge guest-cart удаляется).
    /// </summary>
    public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponseDto>
    {
        private readonly IBuyerRepository _buyerRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IDateTimeProvider _clock;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IBuyerRepository buyerRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ICartRepository cartRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IDateTimeProvider clock,
            ILogger<LoginCommandHandler> logger)
        {
            _buyerRepository = buyerRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _cartRepository = cartRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _clock = clock;
            _logger = logger;
        }

        public async Task<TokenResponseDto> Handle(LoginCommand request, CancellationToken ct)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var buyer = await _buyerRepository.GetByEmailAsync(normalizedEmail, ct);
            if (buyer is null || buyer.Deleted)
                throw new DomainException("Invalid credentials", "UNAUTHORIZED");

            if (!_passwordHasher.Verify(request.Password, buyer.PasswordHash))
                throw new DomainException("Invalid credentials", "UNAUTHORIZED");

            // Выдача токенов.
            var tokens = _tokenService.GenerateTokens(buyer.Id, buyer.Email);

            var refreshToken = new RefreshToken(
                Guid.NewGuid(),
                buyer.Id,
                tokens.RefreshTokenHash,
                _clock.UtcNow.AddSeconds(tokens.ExpiresInSeconds * 4),
                _clock.UtcNow);

            await _refreshTokenRepository.AddAsync(refreshToken, ct);

            // Merge гостевой корзины — отделено в private-метод для читаемости.
            if (!string.IsNullOrWhiteSpace(request.SessionId))
                await MergeGuestCartAsync(buyer.Id, request.SessionId, ct);

            return new TokenResponseDto(
                tokens.AccessToken,
                tokens.RefreshToken,
                tokens.ExpiresInSeconds,
                "Bearer");
        }

        /// <summary>
        /// US-CART-03: guest_cart_merged_on_login.
        /// При конфликте по SkuId берётся MAX(guest, user) — это инвариант Domain (Cart.Merge).
        /// </summary>
        private async Task MergeGuestCartAsync(Guid buyerId, string sessionId, CancellationToken ct)
        {
            var guestCart = await _cartRepository.GetBySessionAsync(sessionId, ct);
            if (guestCart is null || guestCart.Items.Count == 0)
                return;  // нечего мерджить

            var userCart = await _cartRepository.GetByBuyerAsync(buyerId, ct);
            if (userCart is null)
            {
                // Lazy upsert: пользовательской корзины ещё нет — создаём.
                userCart = B2C.Domain.Carts.Cart.ForBuyer(buyerId);
                await _cartRepository.AddAsync(userCart, ct);
            }

            userCart.Merge(guestCart);
            _cartRepository.Remove(guestCart);

            _logger.LogInformation(
                "Merged guest cart {GuestId} into user cart {UserId} for buyer {BuyerId}",
                guestCart.Id, userCart.Id, buyerId);
        }
    }
}
