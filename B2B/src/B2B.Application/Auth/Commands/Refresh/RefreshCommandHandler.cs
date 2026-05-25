using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Auth.Dtos;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Common;
using B2B.Domain.Sellers;
using MediatR;

namespace B2B.Application.Auth.Commands.Refresh
{
    public sealed class RefreshCommandHandler
    : IRequestHandler<RefreshCommand, TokenResponseDto>
    {
        private readonly ISellerRepository _sellerRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public RefreshCommandHandler(
            ISellerRepository sellerRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock)
        {
            _sellerRepository = sellerRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<TokenResponseDto> Handle(
            RefreshCommand request,
            CancellationToken ct)
        {
            // Ищем по хэшу предъявленного токена
            var hash = _tokenService.HashRefreshToken(request.RefreshToken);
            var stored = await _refreshTokenRepository.GetByHashAsync(hash, ct);

            // Нет токена / отозван / истёк → 401
            if (stored is null || !stored.IsActive(_clock.UtcNow))
                throw new DomainException("Invalid or expired refresh token", "UNAUTHORIZED");

            // Продавец должен существовать и быть активным
            var seller = await _sellerRepository.GetByIdAsync(stored.SellerId, ct);
            if (seller is null || seller.Deleted)
                throw new DomainException("Invalid or expired refresh token", "UNAUTHORIZED");

            // РОТАЦИЯ: старый токен отзываем (одноразовость)
            stored.Revoke();

            // Выпускаем новую пару + сохраняем хэш нового refresh
            var pair = _tokenService.GenerateTokens(seller.Id, seller.Email);
            var newToken = new RefreshToken(
                id: Guid.NewGuid(),
                sellerId: seller.Id,
                tokenHash: pair.RefreshTokenHash,
                expiresAt: _clock.UtcNow.AddDays(30),
                createdAt: _clock.UtcNow);

            await _refreshTokenRepository.AddAsync(newToken, ct);

            // stored.Revoke() + новый токен — одна транзакция
            await _unitOfWork.SaveChangesAsync(ct);

            return new TokenResponseDto(
                UserId: seller.Id,
                AccessToken: pair.AccessToken,
                RefreshToken: pair.RefreshToken,
                TokenType: "Bearer",
                ExpiresIn: pair.ExpiresInSeconds);
        }
    }
}
