using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;
using B2C.Domain.Common;
using MediatR;

namespace B2C.Application.Catalog.Queries.GetProductDetail
{
    public sealed class GetProductDetailQueryHandler
        : IRequestHandler<GetProductDetailQuery, CatalogProductDetailDto>
    {
        private readonly IB2BCatalogClient _b2bCatalog;

        public GetProductDetailQueryHandler(IB2BCatalogClient b2bCatalog)
        {
            _b2bCatalog = b2bCatalog;
        }

        public async Task<CatalogProductDetailDto> Handle(
            GetProductDetailQuery request, CancellationToken ct)
        {
            var product = await _b2bCatalog.GetProductAsync(request.ProductId, ct)
                ?? throw new DomainException("Product not found", "NOT_FOUND");

            return CatalogMapper.ToDetail(product);
        }
    }
}
