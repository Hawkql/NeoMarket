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

            var product = await _productRepository.GetByIdAsync(sku.ProductId, ct);
            if (product is null)
                throw new DomainException("SKU not found", "NOT_FOUND");

            // чужой → 403 NOT_OWNER
            if (product.SellerId != request.SellerId)
                throw new DomainException(
                    "SKU does not belong to the authenticated seller", "NOT_OWNER");

            // HARD_BLOCKED → 403 FORBIDDEN
            product.EnsureCanDeleteSku();

            // живые SKU до удаления (для условия «последний»)
            var liveSkusBefore = await _skuRepository.CountByProductIdAsync(product.Id, ct);

            // reserved>0 → 409 CONFLICT
            sku.MarkAsDeleted();

            // товар в витрине + был остаток → SKU_OUT_OF_STOCK в B2C
            if (product.Status == ProductStatus.Moderated)
                sku.RaiseOutOfStockOnRemoval();

            // последний SKU убрали у товара на модерации → возврат в CREATED
            if (liveSkusBefore == 1)
                product.RevertToCreatedOnLastSkuRemoved();

            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
