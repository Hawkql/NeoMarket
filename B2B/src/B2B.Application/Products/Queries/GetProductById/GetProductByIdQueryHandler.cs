using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Domain.Products;
using MediatR;

namespace B2B.Application.Products.Queries.GetProductById
{
    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
    {
        private readonly IProductRepository _productRepository;

        public GetProductByIdQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdProduct(request.Id, ct);

            if (product == null)
                return null;

            return new ProductDto(
                Id: product.Id,
                Title: product.Title,
                Description: product.Description,
                Slug: product.Slug,
                CategoryId: product.CategoryId,
                Status: (int)product.Status,
                Skus: product.Skus.Select(s => new SkuDto(
                    s.Id,
                    s.Name,
                    s.Price,
                    s.Quantity,
                    s.Characteristics.Select(c => new CharacteristicDto(c.Name, c.Value)).ToList()
                )).ToList(),
                Characteristics: product.Characteristics
                    .Select(c => new CharacteristicDto(c.Name, c.Value)).ToList(),
                Images: product.Images
                    .Select(i => new ImageDto(i.Url, i.Order)).ToList());
        }
    }
}
