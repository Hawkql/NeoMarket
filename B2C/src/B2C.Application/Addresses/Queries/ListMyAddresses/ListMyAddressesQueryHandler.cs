using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Addresses.Dtos;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Addresses;
using MediatR;

namespace B2C.Application.Addresses.Queries.ListMyAddresses
{
    public sealed class ListMyAddressesQueryHandler
       : IRequestHandler<ListMyAddressesQuery, IReadOnlyList<AddressDto>>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly ICurrentUserService _currentUser;

        public ListMyAddressesQueryHandler(
            IAddressRepository addressRepository,
            ICurrentUserService currentUser)
        {
            _addressRepository = addressRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<AddressDto>> Handle(
            ListMyAddressesQuery request, CancellationToken ct)
        {
            var addresses = await _addressRepository.ListByBuyerAsync(_currentUser.BuyerId, ct);

            // Сортировка на уровне Application: default первым, потом по дате создания DESC.
            // В Domain-репозитории не сортируем — это presentation-логика.
            return addresses
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .Select(AddressDtoMapper.ToDto)
                .ToList();
        }
    }
}
