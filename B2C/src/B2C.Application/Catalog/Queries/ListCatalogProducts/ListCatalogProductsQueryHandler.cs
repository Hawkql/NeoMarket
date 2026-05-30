using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using B2C.Application.Common.Pagination;
using B2C.Application.Integration;
using B2C.Application.Integration.Dtos;

using MediatR;

namespace B2C.Application.Catalog.Queries.ListCatalogProducts
{
    /// <summary>
    /// Шаги:
    ///   1. Замапить API-Query → Integration.CatalogQuery.
    ///   2. Вызвать B2B через IB2BCatalogClient.
    ///   3. Замапить Integration.ProductSummary[] → CatalogProductCardDto[].
    /// 
    /// Никакой бизнес-логики — чистый proxy + DTO mapping.
    /// B2B сам делает фильтрацию по MODERATED + deleted=false + InStock (см. B2C задание).
    /// </summary>
    public sealed class ListCatalogProductsQueryHandler
        : IRequestHandler<ListCatalogProductsQuery, PagedResult<CatalogProductCardDto>>
    {
        private readonly IB2BCatalogClient _b2bCatalog;

        public ListCatalogProductsQueryHandler(IB2BCatalogClient b2bCatalog)
        {
            _b2bCatalog = b2bCatalog;
        }

        public async Task<PagedResult<CatalogProductCardDto>> Handle(
            ListCatalogProductsQuery request, CancellationToken ct)
        {
            var integrationQuery = new CatalogQuery(
                request.CategoryId,
                request.Search,
                request.Filters,
                request.MinPrice,
                request.MaxPrice,
                CatalogMapper.ToIntegrationSort(request.Sort),
                request.Limit,
                request.Offset);

            var page = await _b2bCatalog.ListProductsAsync(integrationQuery, ct);

            return new PagedResult<CatalogProductCardDto>(
                page.Items.Select(CatalogMapper.ToCard).ToList(),
                page.TotalCount,
                page.Limit,
                page.Offset);
        }
    }
}
