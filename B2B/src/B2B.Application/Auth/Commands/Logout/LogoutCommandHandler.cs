using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Sellers;
using MediatR;

namespace B2B.Application.Auth.Commands.Logout
{
    public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;

        public LogoutCommandHandler(
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            IUnitOfWork unitOfWork)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(LogoutCommand request, CancellationToken ct)
        {
            // Отзываем предъявленный refresh. Идемпотентно: если токена нет или он
            // уже отозван — просто 204 (logout не должен падать). Спека: всегда 204.
            var hash = _tokenService.HashRefreshToken(request.RefreshToken);
            var stored = await _refreshTokenRepository.GetByHashAsync(hash, ct);

            if (stored is not null && !stored.Revoked)
            {
                stored.Revoke();
                await _unitOfWork.SaveChangesAsync(ct);
            }
            // нет токена / уже отозван → ничего не делаем, всё равно 204
        }
    }
}
