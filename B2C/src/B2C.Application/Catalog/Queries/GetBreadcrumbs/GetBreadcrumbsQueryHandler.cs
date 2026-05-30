using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Integration;
using MediatR;

namespace B2C.Application.Catalog.Queries.GetBreadcrumbs
{
    public sealed class GetBreadcrumbsQueryHandler
       : IRequestHandler<GetBreadcrumbsQuery, IReadOnlyList<BreadcrumbDto>>
    {
        private readonly IB2BCatalogClient _b2bCatalog;

        public GetBreadcrumbsQueryHandler(IB2BCatalogClient b2bCatalog)
        {
            _b2bCatalog = b2bCatalog;
        }

        public async Task<IReadOnlyList<BreadcrumbDto>> Handle(
            GetBreadcrumbsQuery request, CancellationToken ct)
        {
            var crumbs = await _b2bCatalog.GetBreadcrumbsAsync(
                request.CategoryId, request.ProductId, ct);

            return crumbs.Select(CatalogMapper.ToBreadcrumb).ToList();
        }
    }
}
