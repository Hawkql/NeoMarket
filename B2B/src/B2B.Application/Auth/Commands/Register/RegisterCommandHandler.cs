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

namespace B2B.Application.Auth.Commands.Register
{
    public sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, TokenResponseDto>
    {
        private readonly ISellerRepository _sellerRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _clock;

        public RegisterCommandHandler(
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
            RegisterCommand request,
            CancellationToken ct)
        {
            // Email занят → 409 (уникальность также защищена unique index в БД)
            if (await _sellerRepository.EmailExistsAsync(request.Email, ct))
                throw new DomainException("Email already registered", "CONFLICT");

            // Хэшируем пароль в Application (Domain хранит готовый хэш)
            var passwordHash = _passwordHasher.Hash(request.Password);

            var seller = Seller.Create(
                email: request.Email,
                passwordHash: passwordHash,
                firstName: request.FirstName,
                lastName: request.LastName,
                middleName: request.MiddleName,
                companyName: request.CompanyName,
                inn: request.Inn,
                phone: request.Phone);

            await _sellerRepository.AddAsync(seller, ct);

            // Выпускаем пару токенов + сохраняем хэш refresh
            var tokens = await IssueTokensAsync(seller, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return tokens;
        }

        private async Task<TokenResponseDto> IssueTokensAsync(Seller seller, CancellationToken ct)
        {
            var pair = _tokenService.GenerateTokens(seller.Id, seller.Email);

            var refreshToken = new RefreshToken(
                id: Guid.NewGuid(),
                sellerId: seller.Id,
                tokenHash: pair.RefreshTokenHash,
                expiresAt: _clock.UtcNow.AddDays(30),
                createdAt: _clock.UtcNow);

            await _refreshTokenRepository.AddAsync(refreshToken, ct);

            return new TokenResponseDto(
                UserId: seller.Id,
                AccessToken: pair.AccessToken,
                RefreshToken: pair.RefreshToken,
                TokenType: "Bearer",
                ExpiresIn: pair.ExpiresInSeconds);
        }
    }
}
