using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Pagination;
using B2B.Application.PublicCatalog.Dtos;
using B2B.Domain.Products;
using MediatR;

namespace B2B.Application.PublicCatalog.Queries.ListPublicProducts
{

    public sealed record ListPublicProductsQuery(
        Guid? CategoryId,
        string? Search,
        int? MinPrice,
        int? MaxPrice,
        Guid? SellerId,
        PublicSort Sort,
        int Limit,
        int Offset
    ) : IRequest<PagedResult<ProductPublicShortDto>>;
}
