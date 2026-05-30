using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;
using MediatR;

namespace B2C.Application.Catalog.Queries.GetFacets
{
    public sealed class GetFacetsQueryHandler
         : IRequestHandler<GetFacetsQuery, CategoryFiltersDto>
    {
        private readonly IB2BCatalogClient _b2bCatalog;

        public GetFacetsQueryHandler(IB2BCatalogClient b2bCatalog)
        {
            _b2bCatalog = b2bCatalog;
        }

        public async Task<CategoryFiltersDto> Handle(GetFacetsQuery request, CancellationToken ct)
        {
            var query = new CatalogQuery(
                request.CategoryId,
                request.Search,
                request.AppliedFilters,
                request.MinPrice,
                request.MaxPrice,
                CatalogSort.Rating,
                Limit: 0,
                Offset: 0);

            var facets = await _b2bCatalog.GetFacetsAsync(query, ct);
            return CatalogMapper.ToCategoryFilters(facets);
        }
    }
}
