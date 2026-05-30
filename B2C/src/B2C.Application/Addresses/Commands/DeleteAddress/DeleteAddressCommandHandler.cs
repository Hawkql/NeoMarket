using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Addresses;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Addresses.Commands.DeleteAddress
{
    public sealed class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly ICurrentUserService _currentUser;

        public DeleteAddressCommandHandler(
            IAddressRepository addressRepository,
            ICurrentUserService currentUser)
        {
            _addressRepository = addressRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(DeleteAddressCommand request, CancellationToken ct)
        {
            var address = await _addressRepository.GetByIdForBuyerAsync(
                request.AddressId, _currentUser.BuyerId, ct)
                ?? throw new DomainException("Address not found", "NOT_FOUND");

            // NB: удаление default-адреса разрешено. В этом случае у покупателя
            // временно нет default — он должен выбрать новый. Это UX-решение
            // (альтернатива — запретить удалять default, что хуже).

            _addressRepository.Remove(address);
        }
    }
}
