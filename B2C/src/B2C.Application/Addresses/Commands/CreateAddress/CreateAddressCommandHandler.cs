using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Addresses.Dtos;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Addresses;
using MediatR;

namespace B2C.Application.Addresses.Commands.CreateAddress
{
    /// <summary>
    /// Шаги:
    ///   1. Address.Create — фабрика домена валидирует поля.
    ///   2. Если IsDefault=true — сбросить флаг у остальных адресов покупателя.
    ///      (Cross-aggregate инвариант "максимум один default" — забота Application.)
    ///   3. Сохранить в репозиторий.
    /// </summary>
    public sealed class CreateAddressCommandHandler
        : IRequestHandler<CreateAddressCommand, AddressDto>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly ICurrentUserService _currentUser;

        public CreateAddressCommandHandler(
            IAddressRepository addressRepository,
            ICurrentUserService currentUser)
        {
            _addressRepository = addressRepository;
            _currentUser = currentUser;
        }

        public async Task<AddressDto> Handle(CreateAddressCommand request, CancellationToken ct)
        {
            var buyerId = _currentUser.BuyerId;

            var address = Address.Create(
                buyerId,
                request.Country,
                request.City,
                request.Street,
                request.House,
                request.Apartment,
                request.PostalCode,
                request.IsDefault);

            await _addressRepository.AddAsync(address, ct);

            if (request.IsDefault)
                await _addressRepository.UnsetDefaultExceptAsync(buyerId, address.Id, ct);

            return AddressDtoMapper.ToDto(address);
        }
    }
}
