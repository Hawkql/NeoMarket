using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Auth.Dtos;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Buyers;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Auth.Commands.Refresh
{
    /// <summary>
    /// JWT refresh rotation:
    ///   1. Хэшируем переданный refreshToken (в БД хранится только хэш).
    ///   2. Ищем по хэшу. Не найден ИЛИ revoked ИЛИ expired → UNAUTHORIZED.
    ///   3. Загружаем покупателя. Удалён → UNAUTHORIZED.
    ///   4. Revoke старого refresh.
    ///   5. Выдаём новую пару access+refresh.
    ///   6. Сохраняем новый RefreshToken.
    /// </summary>
    public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponseDto>
    {
        private readonly IBuyerRepository _buyerRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly IDateTimeProvider _clock;

        public RefreshTokenCommandHandler(
            IBuyerRepository buyerRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            IDateTimeProvider clock)
        {
            _buyerRepository = buyerRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _clock = clock;
        }

        public async Task<TokenResponseDto> Handle(RefreshTokenCommand request, CancellationToken ct)
        {
            var hash = _tokenService.HashRefreshToken(request.RefreshToken);

            var oldToken = await _refreshTokenRepository.GetByHashAsync(hash, ct);
            if (oldToken is null || !oldToken.IsActive(_clock.UtcNow))
                throw new DomainException("Invalid refresh token", "UNAUTHORIZED");

            var buyer = await _buyerRepository.GetByIdAsync(oldToken.BuyerId, ct);
            if (buyer is null || buyer.Deleted)
                throw new DomainException("Account not available", "UNAUTHORIZED");

            // Rotation: старый отзываем, новый выдаём.
            oldToken.Revoke();

            var tokens = _tokenService.GenerateTokens(buyer.Id, buyer.Email);

            var newToken = new RefreshToken(
                Guid.NewGuid(),
                buyer.Id,
                tokens.RefreshTokenHash,
                _clock.UtcNow.AddSeconds(tokens.ExpiresInSeconds * 4),
                _clock.UtcNow);

            await _refreshTokenRepository.AddAsync(newToken, ct);

            return new TokenResponseDto(
                tokens.AccessToken,
                tokens.RefreshToken,
                tokens.ExpiresInSeconds,
                "Bearer");
        }
    }
}
