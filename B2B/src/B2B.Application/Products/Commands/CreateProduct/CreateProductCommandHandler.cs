using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.common.Interface;
using B2B.Domain.Products;
using MediatR;

namespace B2B.Application.Products.Commands.CreateProduct
{
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        public CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
        {
            var product = Product.Create(
                request.Title, 
                request.Description, 
                request.CategoryId, 
                request.SelleryId);
            await _productRepository.AddAsync(product, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return product.Id;
        }
    }
}
