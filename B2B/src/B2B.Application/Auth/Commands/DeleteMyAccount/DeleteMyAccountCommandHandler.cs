using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Common;
using B2B.Domain.Sellers;
using MediatR;

namespace B2B.Application.Auth.Commands.DeleteMyAccount
{
    public sealed class DeleteMyAccountCommandHandler
    : IRequestHandler<DeleteMyAccountCommand>
    {
        private readonly ISellerRepository _sellerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteMyAccountCommandHandler(
            ISellerRepository sellerRepository,
            IUnitOfWork unitOfWork)
        {
            _sellerRepository = sellerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteMyAccountCommand request, CancellationToken ct)
        {
            var seller = await _sellerRepository.GetByIdAsync(request.SellerId, ct);
            if (seller is null || seller.Deleted)
                throw new DomainException("Seller not found", "NOT_FOUND");

            // Soft-delete (спека: «деактивирован»). Товары продавца остаются,
            // но войти он больше не сможет (login фильтрует !Deleted).
            seller.MarkAsDeleted();
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
