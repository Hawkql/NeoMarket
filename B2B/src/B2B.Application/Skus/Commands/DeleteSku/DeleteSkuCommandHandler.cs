using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Domain.Common;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Skus.Commands.DeleteSku
{
    public sealed class DeleteSkuCommandHandler
     : IRequestHandler<DeleteSkuCommand>
    {
        private readonly ISkuRepository _skuRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteSkuCommandHandler(
            ISkuRepository skuRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork)
        {
            _skuRepository = skuRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteSkuCommand request, CancellationToken ct)
        {
            var sku = await _skuRepository.GetByIdAsync(request.SkuId, ct);

            if (sku is null || sku.Deleted)
                throw new DomainException("SKU not found", "NOT_FOUND");

            // Ownership через товар
            var product = await _productRepository.GetByIdAsync(sku.ProductId, ct);
            if (product is null || product.SellerId != request.SellerId)
                throw new DomainException("SKU not found", "NOT_FOUND");

            // Доменная защита: при reserved_quantity > 0 бросит CONFLICT → 409.
            // soft-delete + SkuDeletedEvent.
            sku.MarkAsDeleted();

            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
