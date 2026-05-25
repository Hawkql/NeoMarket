using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Auth.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Sellers;
using MediatR;

namespace B2B.Application.Auth.Queries.GetMyProfile
{
    public sealed class GetMyProfileQueryHandler
    : IRequestHandler<GetMyProfileQuery, SellerResponseDto>
    {
        private readonly ISellerRepository _sellerRepository;

        public GetMyProfileQueryHandler(ISellerRepository sellerRepository)
        {
            _sellerRepository = sellerRepository;
        }

        public async Task<SellerResponseDto> Handle(
            GetMyProfileQuery request,
            CancellationToken ct)
        {
            var seller = await _sellerRepository.GetByIdAsync(request.SellerId, ct);
            if (seller is null || seller.Deleted)
                throw new DomainException("Seller not found", "NOT_FOUND");

            return SellerDtoMapper.Map(seller);
        }
    }
}
