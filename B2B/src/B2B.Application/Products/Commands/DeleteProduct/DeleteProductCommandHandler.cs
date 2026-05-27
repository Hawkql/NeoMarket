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

namespace B2B.Application.Products.Commands.DeleteProduct
{
    public sealed class DeleteProductCommandHandler
    : IRequestHandler<DeleteProductCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly ISkuRepository _skuRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteProductCommandHandler(
            IProductRepository productRepository,
            ISkuRepository skuRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _skuRepository = skuRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteProductCommand request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId, ct);

            // Уже удалён или не существует → 404 (идемпотентность не требуется спекой,
            // повторный DELETE отдаёт 404 — это допустимо)
            if (product is null)
                throw new DomainException("Product not found", "NOT_FOUND");

            // IDOR: чужой товар → 404
            if (product.SellerId != request.SellerId)
                throw new DomainException(
                    "Product does not belong to the authenticated seller", "NOT_OWNER");

            // Собираем sku_ids для события (B2C пометит корзины с этими SKU)
            var skus = await _skuRepository.GetByProductIdAsync(product.Id, ct);
            var skuIds = skus.Select(s => s.Id).ToList();

            // Доменный метод: deleted=true + ProductDeletedEvent.

            product.MarkAsDeleted(skuIds);

            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
