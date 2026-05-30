using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Addresses;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Addresses.Commands.SetDefaultAddress
{
    /// <summary>
    /// Шаги:
    ///   1. Найти адрес (с IDOR-фильтром).
    ///   2. Сбросить флаг у всех остальных адресов покупателя.
    ///   3. Пометить выбранный как default.
    /// 
    /// Порядок 2 → 3 важен: если сначала помечать новый, а потом сбрасывать остальные,
    /// мы можем случайно сбросить новый (если в репозиторий не передан except-id корректно).
    /// </summary>
    public sealed class SetDefaultAddressCommandHandler : IRequestHandler<SetDefaultAddressCommand>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly ICurrentUserService _currentUser;

        public SetDefaultAddressCommandHandler(
            IAddressRepository addressRepository,
            ICurrentUserService currentUser)
        {
            _addressRepository = addressRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(SetDefaultAddressCommand request, CancellationToken ct)
        {
            var buyerId = _currentUser.BuyerId;

            var address = await _addressRepository.GetByIdForBuyerAsync(
                request.AddressId, buyerId, ct)
                ?? throw new DomainException("Address not found", "NOT_FOUND");

            await _addressRepository.UnsetDefaultExceptAsync(buyerId, address.Id, ct);
            address.MarkAsDefault();
        }
    }
}
