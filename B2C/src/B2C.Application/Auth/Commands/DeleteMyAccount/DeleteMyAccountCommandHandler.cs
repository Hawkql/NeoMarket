using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Buyers;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Auth.Commands.DeleteMyAccount
{
    /// <summary>
    /// Шаги:
    ///   1. Загрузить покупателя.
    ///   2. buyer.MarkAsDeleted() — PII обнуляется, статус Deleted=true.
    ///   3. Отозвать ВСЕ refresh-токены — после удаления аккаунта токены не должны работать.
    /// 
    /// NB: cart, favorites, subscriptions покупателя НЕ удаляются автоматически —
    /// это сделает retention-job отдельно (по политике хранения данных).
    /// </summary>
    public sealed class DeleteMyAccountCommandHandler : IRequestHandler<DeleteMyAccountCommand>
    {
        private readonly IBuyerRepository _buyerRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ICurrentUserService _currentUser;

        public DeleteMyAccountCommandHandler(
            IBuyerRepository buyerRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ICurrentUserService currentUser)
        {
            _buyerRepository = buyerRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(DeleteMyAccountCommand request, CancellationToken ct)
        {
            var buyer = await _buyerRepository.GetByIdAsync(_currentUser.BuyerId, ct)
                ?? throw new DomainException("Buyer not found", "NOT_FOUND");

            buyer.MarkAsDeleted();

            // Отзываем все refresh-токены через bulk-метод репозитория.
            // Метода нет в текущем IRefreshTokenRepository — добавляю его (см. правку Domain ниже).
            await _refreshTokenRepository.RevokeAllForBuyerAsync(buyer.Id, ct);
        }
    }
}
