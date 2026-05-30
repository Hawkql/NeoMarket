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

namespace B2C.Application.Auth.Commands.Register
{
    /// <summary>
    /// Шаги:
    ///   1. Проверить, что email не занят (для UX 409, а не криптическая 500 от unique index).
    ///   2. Хэш пароля (IPasswordHasher — реализация в Infrastructure, BCrypt).
    ///   3. Buyer.Register — фабрика создаёт агрегат, валидирует инварианты email.
    ///   4. Сохранить в репозиторий, выдать токены.
    ///   5. Сохранить RefreshToken (отдельный агрегат-like в Buyers/).
    /// 
    /// Транзакция оборачивает всё через TransactionBehavior.
    /// </summary>
    public sealed class BuyerRegisterCommandHandler : IRequestHandler<BuyerRegisterCommand, TokenResponseDto>
    {
        private readonly IBuyerRepository _buyerRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IDateTimeProvider _clock;

        public BuyerRegisterCommandHandler(
            IBuyerRepository buyerRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IDateTimeProvider clock)
        {
            _buyerRepository = buyerRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _clock = clock;
        }

        public async Task<TokenResponseDto> Handle(BuyerRegisterCommand request, CancellationToken ct)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (await _buyerRepository.ExistsByEmailAsync(normalizedEmail, ct))
                throw new DomainException("Email already registered", "CONFLICT");

            var passwordHash = _passwordHasher.Hash(request.Password);

            var buyer = Buyer.Register(
                normalizedEmail, passwordHash,
                request.FirstName, request.LastName, request.Phone);

            await _buyerRepository.AddAsync(buyer, ct);

            // Сразу выдаём токены — автологин после регистрации.
            var tokens = _tokenService.GenerateTokens(buyer.Id, buyer.Email);

            var refreshToken = new RefreshToken(
                System.Guid.NewGuid(),
                buyer.Id,
                tokens.RefreshTokenHash,
                _clock.UtcNow.AddSeconds(tokens.ExpiresInSeconds * 4),  // refresh живёт дольше access
                _clock.UtcNow);

            await _refreshTokenRepository.AddAsync(refreshToken, ct);

            return new TokenResponseDto(
                tokens.AccessToken,
                tokens.RefreshToken,
                tokens.ExpiresInSeconds,
                "Bearer");
        }
    }
}
