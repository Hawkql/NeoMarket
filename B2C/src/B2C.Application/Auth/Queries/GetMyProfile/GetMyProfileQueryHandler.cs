using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Auth.Dtos;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Buyers;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Auth.Queries.GetMyProfile
{
    public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, BuyerProfileDto>
    {
        private readonly IBuyerRepository _buyerRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyProfileQueryHandler(
            IBuyerRepository buyerRepository,
            ICurrentUserService currentUser)
        {
            _buyerRepository = buyerRepository;
            _currentUser = currentUser;
        }

        public async Task<BuyerProfileDto> Handle(GetMyProfileQuery request, CancellationToken ct)
        {
            var buyer = await _buyerRepository.GetByIdAsync(_currentUser.BuyerId, ct)
                ?? throw new DomainException("Buyer not found", "NOT_FOUND");

            return BuyerDtoMapper.ToProfileDto(buyer);
        }
    }
}
