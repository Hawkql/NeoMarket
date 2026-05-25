using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Skus.Dtos;
using MediatR;

namespace B2B.Application.Skus.Queries.ListSkusByProduct
{
    public sealed record ListSkusByProductQuery(
        Guid ProductId,
        Guid SellerId
    ) : IRequest<IReadOnlyList<SkuResponseDto>>;
}
