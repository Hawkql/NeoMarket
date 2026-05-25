using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Products.Dtos;
using B2B.Domain.Common;
using B2B.Domain.Images;
using B2B.Domain.Products;
using B2B.Domain.Skus;
using MediatR;

namespace B2B.Application.Products.Queries.GetProductById
{
    public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
    {
        private readonly IImageRepository _imageRepository;
        private readonly IProductRepository _productRepository;
        private readonly ISkuRepository _skuRepository;
        public GetProductByIdQueryHandler(IImageRepository imageRepository,
            IProductRepository productRepository,
            ISkuRepository skuRepository)
        {
            _imageRepository = imageRepository;
            _productRepository = productRepository;
            _skuRepository = skuRepository;
        }
        public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken ct)
        {
            var product =await _productRepository.GetByIdAsync(request.ProductId, ct);

            // 404 если нет или удалён
            if (product is null || product.Deleted)
                throw new DomainException("Product not found", "NOT_FOUND");

            // IDOR: товар чужого продавца — для него это 404, не 403
            // (не раскрываем факт существования чужого товара)
            if (product.SellerId != request.SellerId)
                throw new DomainException("Product not found", "NOT_FOUND");

            var productImage = await _imageRepository.GetByEntityAsync(ImageEntityType.Product, product.Id, ct);

            var skus = await _skuRepository.GetByProductIdAsync(product.Id, ct);

            var skuIds = skus.Select(s => s.Id).ToList();
            var skuImagesMap = skuIds.Count > 0
                ? await _imageRepository.GetByEntitiesAsync(ImageEntityType.Sku, skuIds, ct)
                : new Dictionary<Guid, IReadOnlyCollection<Image>>();

            return ProductDtoMapper.Map(product, productImage, skus);
        }
    }
}
