using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Integration;
using MediatR;

namespace B2C.Application.Catalog.Queries.GetSimilarProducts
{
    public sealed class GetSimilarProductsQueryHandler
         : IRequestHandler<GetSimilarProductsQuery, IReadOnlyList<CatalogProductCardDto>>
    {
        private readonly IB2BCatalogClient _b2bCatalog;

        public GetSimilarProductsQueryHandler(IB2BCatalogClient b2bCatalog)
        {
            _b2bCatalog = b2bCatalog;
        }

        public async Task<IReadOnlyList<CatalogProductCardDto>> Handle(
            GetSimilarProductsQuery request, CancellationToken ct)
        {
            var similar = await _b2bCatalog.GetSimilarAsync(request.ProductId, request.Limit, ct);
            return similar.Select(CatalogMapper.ToCard).ToList();
        }
    }
}
