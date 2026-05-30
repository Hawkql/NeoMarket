using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;
using MediatR;

namespace B2C.Application.Catalog.Queries.GetCategoryFilters
{
    public sealed class GetCategoryFiltersQueryHandler
         : IRequestHandler<GetCategoryFiltersQuery, CategoryFiltersDto>
    {
        private readonly IB2BCatalogClient _b2bCatalog;

        public GetCategoryFiltersQueryHandler(IB2BCatalogClient b2bCatalog)
        {
            _b2bCatalog = b2bCatalog;
        }

        public async Task<CategoryFiltersDto> Handle(
            GetCategoryFiltersQuery request, CancellationToken ct)
        {
            var filters = await _b2bCatalog.GetCategoryFiltersAsync(request.CategoryId, ct);
            return CatalogMapper.ToCategoryFilters(filters);
        }
    }
}
