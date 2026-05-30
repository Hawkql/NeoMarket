using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Domain.Buyers;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Auth.Commands.UpdateMyProfile
{
    public sealed class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand>
    {
        private readonly IBuyerRepository _buyerRepository;
        private readonly ICurrentUserService _currentUser;

        public UpdateMyProfileCommandHandler(
            IBuyerRepository buyerRepository,
            ICurrentUserService currentUser)
        {
            _buyerRepository = buyerRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(UpdateMyProfileCommand request, CancellationToken ct)
        {
            var buyer = await _buyerRepository.GetByIdAsync(_currentUser.BuyerId, ct)
                ?? throw new DomainException("Buyer not found", "NOT_FOUND");

            buyer.UpdateProfile(request.FirstName, request.LastName, request.Phone);
        }
    }
}
