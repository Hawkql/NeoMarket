using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Addresses;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Addresses.Commands.UpdateAddress
{
    public sealed class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly ICurrentUserService _currentUser;

        public UpdateAddressCommandHandler(
            IAddressRepository addressRepository,
            ICurrentUserService currentUser)
        {
            _addressRepository = addressRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(UpdateAddressCommand request, CancellationToken ct)
        {
            // GetByIdForBuyerAsync уже фильтрует по обоим Id — IDOR закрыт.
            // Если адрес чужой или не существует — возвращается null → NOT_FOUND.
            // Это правильное поведение: для чужого адреса возвращаем 404, не 403,
            // чтобы не палить, что такой Id существует у другого пользователя.
            var address = await _addressRepository.GetByIdForBuyerAsync(
                request.AddressId, _currentUser.BuyerId, ct)
                ?? throw new DomainException("Address not found", "NOT_FOUND");

            address.Update(
                request.Country,
                request.City,
                request.Street,
                request.House,
                request.Apartment,
                request.PostalCode);
        }
    }
}
