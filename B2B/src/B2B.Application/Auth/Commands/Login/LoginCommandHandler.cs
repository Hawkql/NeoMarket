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

namespace B2B.Application.Auth.Commands.Login
{
    public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, TokenResponseDto>
    {
        private readonly ISellerRepository _sellerRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public LoginCommandHandler(
            ISellerRepository sellerRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock)
        {
            _sellerRepository = sellerRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<TokenResponseDto> Handle(
            LoginCommand request,
            CancellationToken ct)
        {
            var seller = await _sellerRepository.GetByEmailAsync(request.Email, ct);

            // Неверный email ИЛИ пароль → одинаковый 401 (не раскрываем, что именно).
            // Verify вызываем даже если seller == null? Нет — null проверяем первым,
            // но это минимальный timing-leak; для нашего scope приемлемо.
            if (seller is null ||
                !_passwordHasher.Verify(request.Password, seller.PasswordHash))
            {
                throw new DomainException("Invalid email or password", "UNAUTHORIZED");
            }

            var pair = _tokenService.GenerateTokens(seller.Id, seller.Email);

            var refreshToken = new RefreshToken(
                id: Guid.NewGuid(),
                sellerId: seller.Id,
                tokenHash: pair.RefreshTokenHash,
                expiresAt: _clock.UtcNow.AddDays(30),
                createdAt: _clock.UtcNow);

            await _refreshTokenRepository.AddAsync(refreshToken, ct);
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
