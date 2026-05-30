using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Addresses.Dtos;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Addresses;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Addresses.Queries.GetMyAddress
{
    public sealed class GetMyAddressQueryHandler : IRequestHandler<GetMyAddressQuery, AddressDto>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyAddressQueryHandler(
            IAddressRepository addressRepository,
            ICurrentUserService currentUser)
        {
            _addressRepository = addressRepository;
            _currentUser = currentUser;
        }

        public async Task<AddressDto> Handle(GetMyAddressQuery request, CancellationToken ct)
        {
            var address = await _addressRepository.GetByIdForBuyerAsync(
                request.AddressId, _currentUser.BuyerId, ct)
                ?? throw new DomainException("Address not found", "NOT_FOUND");

            return AddressDtoMapper.ToDto(address);
        }
    }
}
