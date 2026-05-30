using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Buyers;
using MediatR;

namespace B2C.Application.Auth.Commands.Logout
{
    /// <summary>
    /// Идемпотентный logout: если refresh уже отозван или не существует — всё равно 200.
    /// Это сознательное решение: фронт не должен получать ошибки при logout.
    /// </summary>
    public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;

        public LogoutCommandHandler(
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
        }

        public async Task Handle(LogoutCommand request, CancellationToken ct)
        {
            var hash = _tokenService.HashRefreshToken(request.RefreshToken);
            var token = await _refreshTokenRepository.GetByHashAsync(hash, ct);

            // null или уже Revoked — оба случая трактуем как успех.
            token?.Revoke();
        }
    }
}
