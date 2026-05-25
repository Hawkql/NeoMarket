using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Auth.Dtos;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Common;
using B2B.Domain.Sellers;
using MediatR;

namespace B2B.Application.Auth.Commands.UpdateMyProfile
{
    public sealed class UpdateMyProfileCommandHandler
    : IRequestHandler<UpdateMyProfileCommand, SellerResponseDto>
    {
        private readonly ISellerRepository _sellerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateMyProfileCommandHandler(
            ISellerRepository sellerRepository,
            IUnitOfWork unitOfWork)
        {
            _sellerRepository = sellerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SellerResponseDto> Handle(
            UpdateMyProfileCommand request,
            CancellationToken ct)
        {
            var seller = await _sellerRepository.GetByIdAsync(request.SellerId, ct);
            if (seller is null || seller.Deleted)
                throw new DomainException("Seller not found", "NOT_FOUND");

            // PATCH-merge внутри агрегата (null = не менять)
            seller.UpdateProfile(
                firstName: request.FirstName,
                lastName: request.LastName,
                middleName: request.MiddleName,
                companyName: request.CompanyName,
                phone: request.Phone);

            await _unitOfWork.SaveChangesAsync(ct);

            return SellerDtoMapper.Map(seller);
        }
    }
}
