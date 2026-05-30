using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Buyers;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Auth.Commands.ChangePassword
{
    /// <summary>
    /// Шаги:
    ///   1. Загрузить покупателя (BuyerId из JWT).
    ///   2. Захэшировать новый пароль (IPasswordHasher).
    ///   3. buyer.ChangePassword — Domain валидирует и меняет hash.
    ///   4. Отозвать все refresh-токены — после смены пароля старые сессии недействительны.
    /// </summary>
    public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
    {
        private readonly IBuyerRepository _buyerRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICurrentUserService _currentUser;

        public ChangePasswordCommandHandler(
            IBuyerRepository buyerRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            ICurrentUserService currentUser)
        {
            _buyerRepository = buyerRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _currentUser = currentUser;
        }

        public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
        {
            var buyer = await _buyerRepository.GetByIdAsync(_currentUser.BuyerId, ct)
                ?? throw new DomainException("Buyer not found", "NOT_FOUND");

            var newHash = _passwordHasher.Hash(request.NewPassword);
            buyer.ChangePassword(newHash);

            // Разлогинить все сессии — смена пароля должна инвалидировать старые токены.
            await _refreshTokenRepository.RevokeAllForBuyerAsync(buyer.Id, ct);
        }
    }
}
