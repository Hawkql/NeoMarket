using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Pagination;
using B2B.Application.Products.Dtos;
using B2B.Domain.Products;
using MediatR;

namespace B2B.Application.Products.Queries.ListProducts
{
        public sealed record ListProductsQuery(
        Guid SellerId,
        ProductStatus? Status,
        bool IncludeDeleted,
        int Limit,
        int Offset
    ) : IRequest<PagedResult<ProductShortDto>>;
}
